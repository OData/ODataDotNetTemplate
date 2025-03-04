<#
.SYNOPSIS
  Increments the version number in the specified msbuild props file.

.DESCRIPTION
  This script will increment the version number in the specified msbuild props file. The version number can be incremented in the following ways:
  - Major version increment
  - Minor version increment
  - Revision version increment
  - Version release number increment

.PARAMETER versionPath
  The path to the msbuild props file where the version number is specified.

.PARAMETER lastReleaseCommit
  The ID of the last commit to be released.

.PARAMETER forceMajorIncrement
  Whether to force an increment of the major version number.

.PARAMETER versionRelease
  The version number to be released. For example, preview, beta, alpha, etc.

.PARAMETER forceMinorIncrement
  Whether to force an increment of the minor version number.

.PARAMETER Help
  Show help.

.EXAMPLE
  Increment the minor version number in the Versions.Settings.targets file
  .\IncrementVersion.ps1 -versionPath .\Versions.Settings.targets -lastReleaseCommit 123456

.EXAMPLE
  Increment the minor version number in the Versions.Settings.targets file
  .\IncrementVersion.ps1 -versionPath .\Versions.Settings.targets -forceMinorIncrement

.EXAMPLE
  Increment the major version number in the Versions.Settings.targets file
  .\IncrementVersion.ps1 -versionPath .\Versions.Settings.targets -forceMajorIncrement

.EXAMPLE
  Set the version release number in the Versions.Settings.targets file
  .\IncrementVersion.ps1 -versionPath .\Versions.Settings.targets -versionRelease preview
#>

Param(
  [string]$versionPath,
  [string]$lastReleaseCommit,
  [string]$versionRelease,
  [Switch]$forceMajorIncrement,
  [Switch]$forceMinorIncrement,
  [switch]$Help # Show help
)

# Show help if requested
if ($Help) {
  Get-Help $PSCommandPath
  exit 1
}

# Set default version path if not provided
if ($versionPath -eq "") {
  $versionPath = "$PSScriptRoot\Versions.Settings.targets"
}

# Get the last release commit ID if not provided
if ($lastReleaseCommit -eq "") {
  $lastReleaseCommit = git log -n 1 --pretty=format:%H -- $versionPath
}

Write-Host "The last release's commit was $lastReleaseCommit"

# Get the list of files changed since the last release commit
$changedFiles = git diff --name-only HEAD $lastReleaseCommit

# Function to check if configuration files have changed
function Test-ConfigFilesChanged {
  param (
    [string[]]$changedFiles
  )

  # Check each changed file
  ForEach ($line in $($changedFiles -split [System.Environment]::NewLine)) {
    $fileName = [System.IO.Path]::GetFileName($line)
    # Return true if a configuration file has changed
    if ($fileName -eq "dotnetcli.host.json" -or $fileName -eq "ide.host.json" -or $fileName -eq "template.json") {
      return $true
    }
  }

  return $false
}

# Load the versions file
$versions = New-Object xml
$versions.PreserveWhitespace = $true
$versions.Load($versionPath)

# Iterate through property groups to find version numbers
foreach ($propertyGroup in $versions.Project.PropertyGroup) {
  # Increment major version if requested
  if ($forceMajorIncrement) {
    [int]$currentVersion = $propertyGroup.VersionMajor.'#text'
    $currentVersion++
    $propertyGroup.VersionMajor.InnerText = [string]$currentVersion
    $propertyGroup.VersionMinor.InnerText = '0'
    $propertyGroup.VersionBuild.InnerText = '0'
    $propertyGroup.VersionRelease.InnerText = ''
    $propertyGroup.VersionReleaseNumber.InnerText = ''
    Write-Host "Incrementing the VersionMajor in $versionPath to $currentVersion"
    break
  }

  # Increment minor version if requested or if configuration files have changed
  $configChanged = Test-ConfigFilesChanged -changedFiles $changedFiles
  if ($forceMinorIncrement -or $configChanged) {
    [int]$currentVersion = $propertyGroup.VersionMinor.'#text'
    $currentVersion++
    $propertyGroup.VersionMinor.InnerText = [string]$currentVersion
    $propertyGroup.VersionBuild.InnerText = '0'
    $propertyGroup.VersionRelease.InnerText = ''
    $propertyGroup.VersionReleaseNumber.InnerText = ''
    Write-Host "Incrementing the VersionMinor in $versionPath to $currentVersion"
    break
  }

  # Set version release if requested
  if ($versionRelease) {
    $versionReleaseNumber = 1
    $versionRelease = $versionRelease.ToLower()
    if ($null -ne $propertyGroup.VersionRelease) {
      if ($propertyGroup.VersionRelease.'#text' -ne $versionRelease) {
        $propertyGroup.VersionRelease.InnerText = $versionRelease
        $propertyGroup.VersionReleaseNumber.InnerText = '1'
      } else {
        $currentVersionRelease = [int]$propertyGroup.VersionReleaseNumber.'#text'
        $propertyGroup.VersionReleaseNumber.InnerText = [string]($currentVersionRelease + 1)
      }
      Write-Host "Setting the VersionReleaseNumber in $versionPath to $versionReleaseNumber"
    } else {
      $propertyGroup.VersionRelease.InnerText = $versionRelease
      $propertyGroup.VersionReleaseNumber.InnerText = '1'
    }
    break
  }

  # Increment build version if no other increments are requested
  [int]$currentVersion = $propertyGroup.VersionBuild.'#text'
  $currentVersion++
  $propertyGroup.VersionBuild.'#text' = [string]$currentVersion
  $propertyGroup.VersionRelease.InnerText = ''
  $propertyGroup.VersionReleaseNumber.InnerText = ''
  Write-Host "Incrementing the VersionBuild in $versionPath to $currentVersion"
  break
}

# Save the updated versions file
$versions.Save($versionPath)
