//---------------------------------------------------------------------
// <copyright file="AspNetProcess.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace ODataWebApiTemplate.Tests.Helpers;

[DebuggerDisplay("{ToString(),nq}")]
public class AspNetProcess : IDisposable
{
    private const string ListeningMessagePrefix = "Now listening on: ";
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;

    internal readonly Uri? ListeningUri;
    internal ProcessEx Process { get; }

    public AspNetProcess(
       ITestOutputHelper output,
       string workingDirectory,
       string dllPath,
       IDictionary<string, string> environmentVariables,
       bool hasListeningUri = true,
       ILogger? logger = null)
    {
        _output = output;
        _httpClient = new HttpClient(new HttpClientHandler())
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        output.WriteLine("Running ASP.NET Core OData application...");

        var process = DotNetMuxer.MuxerPathOrDefault();

        // When executing "dotnet run", the launch urls specified in the app's launchSettings.json have higher precedence
        // than ambient environment variables. We specify the urls using command line arguments instead to allow us
        // to continue binding to "port 0" and avoid test flakiness due to port conflicts.
        var arguments = $"run --no-build --urls \"{environmentVariables["ASPNETCORE_URLS"]}\"";

        if (logger is not null && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation($"AspNetProcess - process: {process} arguments: {arguments}");
        }

        Process = ProcessEx.Run(output, workingDirectory, process, arguments);

        logger?.LogInformation("AspNetProcess - process started");

        if (hasListeningUri)
        {
            logger?.LogInformation("AspNetProcess - Getting listening uri");
            ListeningUri = ResolveListeningUrl(output);
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger?.LogInformation($"AspNetProcess - Got {ListeningUri}");
            }
        }
    }

    private Uri? ResolveListeningUrl(ITestOutputHelper output)
    {
        // Wait until the app is accepting HTTP requests
        output.WriteLine("Waiting until ASP.NET application is accepting connections...");
        var listeningMessage = GetListeningMessage();

        if (!string.IsNullOrEmpty(listeningMessage))
        {
            listeningMessage = listeningMessage.Trim();
            // Verify we have a valid URL to make requests to
            var listeningUrlString = listeningMessage.Substring(listeningMessage.IndexOf(
                ListeningMessagePrefix, StringComparison.Ordinal) + ListeningMessagePrefix.Length);

            output.WriteLine($"Detected that ASP.NET application is accepting connections on: {listeningUrlString}");
            listeningUrlString = string.Concat(listeningUrlString.AsSpan(0, listeningUrlString.IndexOf(':')),
                "://localhost",
                listeningUrlString.AsSpan(listeningUrlString.LastIndexOf(':')));

            output.WriteLine("Sending requests to " + listeningUrlString);
            return new Uri(listeningUrlString, UriKind.Absolute);
        }
        else
        {
            return null;
        }
    }

    private string GetListeningMessage()
    {
        var buffer = new List<string>();
        try
        {
            Assert.NotNull(Process.OutputLinesAsEnumerable);
            foreach (var line in Process.OutputLinesAsEnumerable)
            {
                if (line != null)
                {
                    buffer.Add(line);
                    if (line.Trim().Contains(ListeningMessagePrefix, StringComparison.Ordinal))
                    {
                        return line;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        throw new InvalidOperationException(@$"Couldn't find listening url: {string.Join(Environment.NewLine, buffer)}");
    }

    public Task AssertOk(string requestUrl)
        => AssertStatusCode(requestUrl, HttpStatusCode.OK);

    public Task AssertNotFound(string requestUrl)
        => AssertStatusCode(requestUrl, HttpStatusCode.NotFound);

    public Task AssertBadRequest(string requestUrl)
        => AssertStatusCode(requestUrl, HttpStatusCode.BadRequest);

    public Task AssertStatusCodeForPostRequest(string requestUrl, string body)
        => AssertStatusCodeForAddOrUpdateRequest(requestUrl, HttpMethod.Post, HttpStatusCode.Created, body);

    public Task AssertStatusCodeForPatchRequest(string requestUrl, string body)
        => AssertStatusCodeForAddOrUpdateRequest(requestUrl, HttpMethod.Patch, HttpStatusCode.OK, body);

    public Task AssertStatusCodeForODataBatching(string requestUrl, string body)
        => AssertStatusCodeForAddOrUpdateRequest(requestUrl, HttpMethod.Post, HttpStatusCode.OK, body);

    public async Task AssertStatusCode(string requestUrl, HttpStatusCode statusCode)
    {
        Assert.NotNull(ListeningUri);

        var response = await RetryHelper.RetryRequest(() =>
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(ListeningUri, requestUrl));

            request.Headers.Add("Accept", "application/json;odata.metadata=full");

            return _httpClient.SendAsync(request);
        }, logger: NullLogger.Instance);

        Assert.True(statusCode == response.StatusCode, 
            $"Expected {requestUrl} to have status '{statusCode}' but it was '{response.StatusCode}'.");
    }

    public async Task AssertStatusCodeForAddOrUpdateRequest(
        string requestUrl, HttpMethod httpMethod,
        HttpStatusCode httpStatusCode,
        string body)
    {
        Assert.NotNull(ListeningUri);
        var response = await RetryHelper.RetryRequest(() =>
        {
            var request = new HttpRequestMessage(
                httpMethod,
                new Uri(ListeningUri, requestUrl));

            request.Headers.Add("Accept", "application/json;odata.metadata=full");
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            return _httpClient.SendAsync(request);
        }, logger: NullLogger.Instance);

        Assert.True(httpStatusCode == response.StatusCode,
            $"Expected {requestUrl} to have status '{httpStatusCode}' but it was '{response.StatusCode}'.");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        Process.Dispose();
    }

    public override string ToString()
    {
        var result = "";
        result += Process != null ? "Active: " : "Inactive";
        if (Process != null)
        {
            if (!Process.HasExited)
            {
                result += $"(Listening on {ListeningUri?.OriginalString}) PID: {Process.Id}";
            }
            else
            {
                result += "(Already finished)";
            }
        }

        return result;
    }
}
