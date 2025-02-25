<#
.SYNOPSIS
Builds, packs and tests the AspNetCoreOData Template project.

.DESCRIPTION
This script automates the process of building, creating NuGet packages and testing for AspNetCoreOData template project. 
It handles NuGet package restoration, project building, test execution (optional), and NuGet package creation.  
It supports different build configurations (Debug/Release) and verbosity levels for logging.

.PARAMETER SolutionPath
The path to the .sln file.  Defaults to ".\sln\YourSolution.sln".

.PARAMETER ArtifactsPath
The base output path for build artifacts.  Defaults to ".\artifacts".  
Log files are stored in ".\artifacts\log", 
NuGet packages in ".\artifacts\package\$Configuration", and test results in ".\artifacts\test-results".

.PARAMETER NightlyBuild
A switch to enable nightly build. If present, the build will be treated as a nightly build.

.PARAMETER Configuration
The build configuration (Debug or Release). Defaults to "Debug".  Use the alias "-c".

.PARAMETER Test
A switch to enable test execution. If present, tests will be run.

.PARAMETER LogFilePath
The path to the log file. Defaults to ".\artifacts\log\build.log".

.PARAMETER Verbosity
The verbosity level for logging.  
Valid values are Quiet, Minimal, Normal, Detailed, and Diagnostic. Defaults to Minimal. Use the alias "-v".

.EXAMPLE
Builds the solution with default settings (Release configuration, minimal verbosity, no tests):

    build.ps1

.EXAMPLE
Builds the solution in Debug configuration and runs tests with detailed verbosity:

    build.ps1 -SolutionPath ".\sln\MySolution.sln" -c "Debug" -test -v "Detailed"

.EXAMPLE
Builds the solution and creates NuGet packages:

    build.ps1 -SolutionPath ".\sln\MySolution.sln" 

.EXAMPLE
Running tests.

    build.ps1 -test

.EXAMPLE
Running nightly build.

    build.ps1 -nightlybuild

#>
param(
    [string]$SolutionPath,
    [string]$ArtifactsPath,
    [string]$LogFilePath,

    [switch]$Test,
    [switch]$Help, # Show help
    [switch]$NightlyBuild,

    [Alias('c')]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = "Debug",

    [Alias('v')]
    [ValidateSet("Quiet", "Minimal", "Normal", "Detailed", "Diagnostic")]
    [string]$Verbosity = "Minimal"
)

if ($Help) {
    Get-Help $PSCommandPath
    exit 1
}

if($SolutionPath -eq $null -or $SolutionPath -eq "") {
    $SolutionPath = "$PSScriptRoot\sln\ODataDotNetTemplate.sln"
}

if($ArtifactsPath -eq $null -or $ArtifactsPath -eq "") {
    $ArtifactsPath = "$PSScriptRoot\artifacts"
}

$now = Get-Date -Format "yyyyMMddHHmmss"

if($LogFilePath -eq $null -or $LogFilePath -eq "") {
    $LogFilePath = "$ArtifactsPath\log\build_$now.log"
}

# Create output directories
New-Item -ItemType Directory -Path "$ArtifactsPath\log" -Force | Out-Null
New-Item -ItemType Directory -Path "$ArtifactsPath\package\$Configuration" -Force | Out-Null
New-Item -ItemType Directory -Path "$ArtifactsPath\test-results" -Force | Out-Null

# Start logging
Start-Transcript -Path $LogFilePath -Append

Write-Host "Build script started at: $(Get-Date)"

Write-Host "Restoring NuGet packages..."
dotnet restore $SolutionPath --verbosity $Verbosity

Write-Host "Building and packing the project..."
if($NightlyBuild) {
    Write-Host "Running nightly build... `ndotnet build $SolutionPath -c $Configuration -p:GeneratePackageOnBuild=true -p:IsNightlyBuild=true --verbosity $Verbosity"
    dotnet build $SolutionPath -c $Configuration -p:GeneratePackageOnBuild=true -p:IsNightlyBuild=true --verbosity $Verbosity
}
else {
    Write-Host "Running regular build... `ndotnet build $SolutionPath -c $Configuration -p:GeneratePackageOnBuild=true --verbosity $Verbosity"
    dotnet build $SolutionPath -c $Configuration -p:GeneratePackageOnBuild=true --verbosity $Verbosity
}

# Run tests if the -Test switch is present
if ($Test) {
    Write-Host "Running tests..."
    try {
        $TestProject = "$PSScriptRoot\test\ODataWebApiTemplate.Tests\ODataWebApiTemplate.Tests.csproj"
        Write-Host "Running tests for $($TestProject)..." -Level "Detailed"
        dotnet test $TestProject -l:"trx;LogFileName=$ArtifactsPath\test-results\Test-Results-ODataWebApiTemplate.Tests-$now.trx" --no-restore --configuration $Configuration --verbosity $Verbosity
        Write-Host "Tests for $($SolutionPath) completed successfully."
    }
    catch {
        Write-Host "Tests for $($TestProject) failed: $_" -Level "Detailed" -Error
        exit 1
    }
} else {
    Write-Host "Skipping tests."
}

# Pack the projects
Write-Host "Packing the projects..."
foreach ($ProjectPath in $ProjectPaths) {
    if ($ProjectPath -notlike "*Tests*") {
        Write-Host "Packing project $($ProjectPath)..." -Level "Detailed"
        dotnet pack $ProjectPath -c $Configuration --verbosity $Verbosity
        Write-Host "NuGet package created for $($ProjectPath) at: $ArtifactsPath\package\$Configuration"
    }
}

Write-Host "Build and pack process completed."

Write-Host "Build script finished at: $(Get-Date)"

# Stop logging
Stop-Transcript