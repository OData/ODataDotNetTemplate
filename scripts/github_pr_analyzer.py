import os
import requests
from datetime import datetime, timezone
from dateutil import parser

# Configuration
SYSTEM_ACCESS_TOKEN = os.getenv('SYSTEM_ACCESSTOKEN')
GITHUB_REPO = os.getenv('GITHUB_REPO')
TEAMS_WEBHOOK_URL = os.getenv('TEAMS_WEBHOOK_URL')
GITHUB_API_URL = "https://api.github.com"


def make_github_request(url):
    """Helper function to make authenticated requests to GitHub API."""
    headers = {
        "Authorization": f"Bearer {SYSTEM_ACCESS_TOKEN}",
        "Accept": "application/vnd.github.v3+json",
        "X-GitHub-Api-Version": "2022-11-28"
    }
    response = requests.get(url, headers=headers)
    response.raise_for_status()
    return response.json()


def get_github_prs():
    """Fetches all open PRs for the repository."""
    url = f"{GITHUB_API_URL}/repos/{GITHUB_REPO}/pulls?state=open&sort=created&direction=asc&per_page=100"
    return make_github_request(url)


def get_pr_comments(pr_number):
    """Fetches the comments for a specific PR."""
    url = f"{GITHUB_API_URL}/repos/{GITHUB_REPO}/pulls/{pr_number}/comments?sort=created&direction=desc&per_page=3"
    return make_github_request(url)


def analyze_comments(comments):
    """Analyzes the last few comments to determine PR status.

    Always returns a tuple: (status, context)
    """
    if not comments:
        return ("No comments", "No one has reviewed this PR.")

    last_comment = comments[0]
    author = last_comment.get('user', {}).get('login', 'unknown')
    created_at = last_comment.get('created_at')
    try:
        comment_date = parser.parse(created_at).astimezone(timezone.utc)
    except Exception:
        comment_date = datetime.now(timezone.utc)

    now = datetime.now(timezone.utc)
    hours_since_comment = (now - comment_date).total_seconds() / 3600
    body = last_comment.get('body') or ''
    preview = (body[:100] + '...') if len(body) > 100 else body

    if hours_since_comment < 48:
        status = "Recent activity"
        context = f"Last comment by _{author}_ ~{int(hours_since_comment)} hours ago:_ \"{preview}\""
    else:
        status = "Stalled"
        context = f"Waiting for author response? Last comment by _{author}_ ~{int(hours_since_comment//24)} days ago:_ \"{preview}\""

    return status, context


def get_pr_details(pr_number):
    """Gets detailed PR information including body."""
    url = f"{GITHUB_API_URL}/repos/{GITHUB_REPO}/pulls/{pr_number}"
    return make_github_request(url)


def categorize_pr(pr_data):
    """Categorizes a PR based on its creation date."""
    created_at = parser.parse(pr_data['created_at']).astimezone(timezone.utc)
    now = datetime.now(timezone.utc)
    days_open = (now - created_at).days

    if days_open >= 30:
        return "🟥 > 1 Month", days_open, "high"
    elif days_open >= 21:
        return "🟧 3 Weeks", days_open, "medium"
    elif days_open >= 14:
        return "🟨 2 Weeks", days_open, "medium"
    elif days_open >= 7:
        return "🟩 1 Week", days_open, "low"
    else:
        return "🟦 < 1 Week", days_open, "low"


def create_teams_message(prs_with_analysis):
    """Creates a rich Microsoft Teams MessageCard payload summarizing PRs."""
    facts = []

    for pr in prs_with_analysis:
        pr_info = f"[{pr['title']}]({pr['html_url']}) by _{pr['author']}_ (**{pr['days_open']} days open** days old)"

        pr_info += f"<br>"
        pr_info += f"**{pr['comment_status']}**: {pr['comment_context']}"

        facts.append({
            "name": f"{pr['category']} | #{pr['number']}",
            "value": pr_info
        })

    card_payload = {
        "@type": "MessageCard",
        "@context": "http://schema.org/extensions",
        "summary": f"GitHub PR Report",
        "themeColor": "0078D7",
        "title": f"GitHub PR Report",
        "text": f"Analysis of {len(prs_with_analysis)} open PRs in **{GITHUB_REPO}**",
        "sections": [{"facts": facts}],
        "potentialAction": [
            {
                "@type": "OpenUri",
                "name": "View All PRs on GitHub",
                "targets": [{"os": "default", "uri": f"https://github.com/{GITHUB_REPO}/pulls"}]
            }
        ]
    }

    return card_payload


def send_to_teams(card_payload):
    """Sends the formatted MessageCard to the Teams webhook."""
    if not TEAMS_WEBHOOK_URL:
        print("TEAMS_WEBHOOK_URL not set. Skipping Teams notification.")
        return

    try:
        resp = requests.post(TEAMS_WEBHOOK_URL, json=card_payload, timeout=15)
        resp.raise_for_status()
    except requests.exceptions.RequestException as e:
        print(f"Failed to send message to Teams: {e}")


def main():
    print("Starting GitHub PR analysis...")

    # Fetch all open PRs
    pr_list = get_github_prs()
    if not pr_list:
        print("No open PRs found. Hooray!")
        return

    print(f"Found {len(pr_list)} open PRs. Performing analysis...")
    prs_with_analysis = []

    for pr in pr_list:
        pr_number = pr['number']

        # Get the last few comments and details
        comments = get_pr_comments(pr_number)

        # Categorize by age
        category, days_open, priority = categorize_pr(pr)

        # Analyze comments (always returns status, context)
        comment_status, comment_context = analyze_comments(comments)

        prs_with_analysis.append({
            'number': pr_number,
            'title': pr['title'],
            'html_url': pr['html_url'],
            'author': pr['user']['login'],
            'days_open': days_open,
            'category': category,
            'priority': priority,
            'comment_status': comment_status,
            'comment_context': comment_context
        })

        # Small delay to avoid rate limiting
        import time
        time.sleep(1)

    # Create and send the report
    print("Creating MS Teams message...")
    teams_payload = create_teams_message(prs_with_analysis)

    print("Sending MS Teams report...")
    send_to_teams(teams_payload)
    print("MS Teams report sent successfully!")


if __name__ == "__main__":
    main()
