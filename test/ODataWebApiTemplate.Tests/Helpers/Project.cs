//---------------------------------------------------------------------
// <copyright file="Project.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ODataWebApiTemplate.Tests.Attributes;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Represents a project used in tests, providing methods to create, build, and run the project, as well as verify its properties and output.
/// </summary>

[DebuggerDisplay("{ToString(),nq}")]
public class Project : IDisposable
{
    private const string _urlNoHttps = "http://127.0.0.1:0";

    /// <summary>
    /// Gets the directory for storing artifact logs.
    /// </summary>
    public static string ArtifactsLogDir
    {
        get
        {
            var testLogFolder = typeof(Project).Assembly.GetCustomAttribute<TestFrameworkFileLoggerAttribute>()?.BaseDirectory;
            if (string.IsNullOrEmpty(testLogFolder))
            {
                throw new InvalidOperationException($"No test log folder specified via {nameof(TestFrameworkFileLoggerAttribute)}.");
            }
            return testLogFolder;
        }
    }

    public string? ProjectName { get; set; }
    public string? ProjectArguments { get; set; }
    public string? ProjectGuid { get; set; }
    public string? TemplateOutputDir { get; set; }
    public string? TargetFramework { get; set; }
    public string? RuntimeIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets the directory where the project is built.
    /// </summary>
    public string TemplateBuildDir
    {
        get
        {
            Assert.NotNull(TemplateOutputDir);
            Assert.NotNull(TargetFramework);
            Assert.NotNull(RuntimeIdentifier);
            return Path.Combine(TemplateOutputDir, "bin", "Debug", TargetFramework, RuntimeIdentifier);
        }
    }

    public ITestOutputHelper? Output { get; set; }

    public IMessageSink? DiagnosticsMessageSink { get; set; }

    /// <summary>
    /// Runs the 'dotnet new' command to create a new project from a template.
    /// </summary>
    /// <param name="templateName">The name of the template to use.</param>
    /// <param name="errorOnRestoreError">Indicates whether to throw an error if the restore fails.</param>
    /// <param name="args">Additional arguments for the 'dotnet new' command.</param>
    internal async Task RunDotNetNewAsync(string templateName, bool errorOnRestoreError = true, string[]? args = null)
    {
        Assert.NotNull(Output);

        var hiveArg = $" --debug:disable-sdk-templates --debug:custom-hive \"{TemplatePackageInstaller.CustomTemplateHivePath}\"";
        var argString = $"new {templateName} {hiveArg}";

        if (args != null)
        {
            foreach (var arg in args)
            {
                argString += " " + arg;
            }
        }

        // Save a copy of the arguments used for better diagnostic error messages later.
        // We omit the hive argument and the template output dir as they are not relevant and add noise.
        ProjectArguments = argString.Replace(hiveArg, "");

        argString += $" -o \"{TemplateOutputDir}\"";

        if (Directory.Exists(TemplateOutputDir))
        {
            Output.WriteLine($"Template directory already exists, deleting contents of {TemplateOutputDir}");
            Directory.Delete(TemplateOutputDir, recursive: true);
        }

        using var execution = ProcessEx.Run(Output, AppContext.BaseDirectory, DotNetMuxer.MuxerPathOrDefault(), argString);
        await execution.Exited;

        var result = new ProcessResult(execution);

        // Because dotnet new automatically restores but silently ignores restore errors, need to handle restore errors explicitly
        if (errorOnRestoreError && (execution.Output.Contains("Restore failed.") || execution.Error.Contains("Restore failed.")))
        {
            result.ExitCode = -1;
        }

        Assert.True(0 == result.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", this, result));
    }

    /// <summary>
    /// Runs the 'dotnet build' command to build the project.
    /// </summary>
    /// <param name="packageOptions">Optional package options for the build.</param>
    /// <param name="additionalArgs">Additional arguments for the 'dotnet build' command.</param>
    /// <param name="errorOnBuildWarning">Indicates whether to throw an error if there are build warnings.</param>
    internal async Task RunDotNetBuildAsync(IDictionary<string, string>? packageOptions = null, string? additionalArgs = null, bool errorOnBuildWarning = true)
    {
        Assert.NotNull(Output);

        Output.WriteLine("Building ASP.NET Core OData application...");

        // Avoid restoring as part of build or publish. These projects should have already restored as part of running dotnet new. Explicitly disabling restore
        // should avoid any global contention and we can execute a build or publish in a lock-free way

        Assert.NotNull(TemplateOutputDir);

        using var execution = ProcessEx.Run(Output, TemplateOutputDir, DotNetMuxer.MuxerPathOrDefault(), $"build --no-restore -c Debug /bl {additionalArgs}", packageOptions);
        await execution.Exited;

        var result = new ProcessResult(execution);

        // Fail if there were build warnings
        if (errorOnBuildWarning && (execution.Output.Contains(": warning") || execution.Error.Contains(": warning")))
        {
            result.ExitCode = -1;
        }

        CaptureBinLogOnFailure(execution);

        Assert.True(0 == result.ExitCode, ErrorMessages.GetFailedProcessMessage("build", this, result));
    }

    /// <summary>
    /// Starts the built project as an ASP.NET Core application.
    /// </summary>
    /// <param name="hasListeningUri">Indicates whether the application has a listening URI.</param>
    /// <param name="targetFramework">Target Framework like net6.0, net8.0.</param>
    /// <param name="logger">Optional logger for the application.</param>
    /// <returns>An <see cref="AspNetProcess"/> representing the running application.</returns>
    internal AspNetProcess StartBuiltProjectAsync(bool hasListeningUri = true, string? targetFramework = null, ILogger? logger = null)
    {
        TargetFramework = targetFramework;
        Assert.NotNull(TargetFramework);

        var environment = new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = _urlNoHttps,
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ASPNETCORE_Logging__Console__LogLevel__Default"] = "Debug",
            ["ASPNETCORE_Logging__Console__LogLevel__System"] = "Debug",
            ["ASPNETCORE_Logging__Console__LogLevel__Microsoft"] = "Debug",
            ["ASPNETCORE_Logging__Console__FormatterOptions__IncludeScopes"] = "true",
        };

        var projectDll = Path.Combine(TemplateBuildDir, $"{ProjectName}.dll");

        Assert.NotNull(Output);
        Assert.NotNull(TemplateOutputDir);

        return new AspNetProcess(Output, TemplateOutputDir, projectDll, environment, hasListeningUri: hasListeningUri, logger: logger);
    }

    /// <summary>
    /// Asserts that a file exists or does not exist in the template output directory.
    /// </summary>
    /// <param name="path">The relative path to the file.</param>
    /// <param name="shouldExist">Indicates whether the file should exist.</param>
    public void AssertFileExists(string path, bool shouldExist)
    {
        Assert.NotNull(TemplateOutputDir);

        var fullPath = Path.Combine(TemplateOutputDir, path);
        var doesExist = File.Exists(fullPath);

        if (shouldExist)
        {
            Assert.True(doesExist, "Expected file to exist, but it doesn't: " + path);
        }
        else
        {
            Assert.False(doesExist, "Expected file not to exist, but it does: " + path);
        }
    }

    /// <summary>
    /// Verifies the launch settings of the project.
    /// </summary>
    /// <param name="expectedLaunchProfileNames">The expected launch profile names.</param>
    public async Task VerifyLaunchSettings(string[] expectedLaunchProfileNames)
    {
        Assert.NotNull(TemplateOutputDir);

        var launchSettingsFiles = Directory.EnumerateFiles(TemplateOutputDir, "launchSettings.json", SearchOption.AllDirectories);

        foreach (var filePath in launchSettingsFiles)
        {
            using var launchSettingsFile = File.OpenRead(filePath);
            using var launchSettings = await JsonDocument.ParseAsync(launchSettingsFile);

            var profiles = launchSettings.RootElement.GetProperty("profiles");
            var profilesEnumerator = profiles.EnumerateObject().GetEnumerator();

            foreach (var expectedProfileName in expectedLaunchProfileNames)
            {
                Assert.True(profilesEnumerator.MoveNext());

                var actualProfile = profilesEnumerator.Current;

                // Launch profile names are case sensitive
                Assert.Equal(expectedProfileName, actualProfile.Name, StringComparer.Ordinal);

                if (actualProfile.Value.GetProperty("commandName").GetString() == "Project")
                {
                    var applicationUrl = actualProfile.Value.GetProperty("applicationUrl");
                    if (string.Equals(expectedProfileName, "http", StringComparison.Ordinal))
                    {
                        Assert.DoesNotContain("https://", applicationUrl.GetString());
                    }

                    if (string.Equals(expectedProfileName, "https", StringComparison.Ordinal))
                    {
                        Assert.StartsWith("https://", applicationUrl.GetString());
                    }
                }
            }

            // Check there are no more launch profiles defined
            Assert.False(profilesEnumerator.MoveNext());
        }
    }

    /// <summary>
    /// Verifies that the project file contains a specific property with the expected value.
    /// </summary>
    /// <param name="propertyName">The name of the property to verify.</param>
    /// <param name="expectedValue">The expected value of the property.</param>
    public async Task VerifyHasProperty(string propertyName, string expectedValue)
    {
        Assert.NotNull(TemplateOutputDir);

        var projectFile = Directory.EnumerateFiles(TemplateOutputDir, "*proj").FirstOrDefault();

        Assert.NotNull(projectFile);

        var projectFileContents = await File.ReadAllTextAsync(projectFile);
        Assert.Contains($"<{propertyName}>{expectedValue}</{propertyName}>", projectFileContents);
    }

    /// <summary>
    /// Disposes the project, deleting the output directory.
    /// </summary>
    public void Dispose()
    {
        DeleteOutputDirectory();
    }

    /// <summary>
    /// Deletes the template output directory.
    /// </summary>
    public void DeleteOutputDirectory()
    {
        Assert.NotNull(TemplateOutputDir);
        Assert.NotNull(DiagnosticsMessageSink);

        const int NumAttempts = 10;

        for (var numAttemptsRemaining = NumAttempts; numAttemptsRemaining > 0; numAttemptsRemaining--)
        {
            try
            {
                Directory.Delete(TemplateOutputDir, true);
                return;
            }
            catch (Exception ex)
            {
                if (numAttemptsRemaining > 1)
                {
                    DiagnosticsMessageSink.OnMessage(new DiagnosticMessage($"Failed to delete directory {TemplateOutputDir} because of error {ex.Message}. Will try again {numAttemptsRemaining - 1} more time(s)."));
                    Thread.Sleep(3000);
                }
                else
                {
                    DiagnosticsMessageSink.OnMessage(new DiagnosticMessage($"Giving up trying to delete directory {TemplateOutputDir} after {NumAttempts} attempts. Most recent error was: {ex.StackTrace}"));
                }
            }
        }
    }

    /// <summary>
    /// Represents an ordered lock for process synchronization.
    /// </summary>
    private sealed class OrderedLock
    {
        private bool _nodeLockTaken;
        private bool _dotNetLockTaken;

        public OrderedLock(ProcessLock nodeLock, ProcessLock dotnetLock)
        {
            NodeLock = nodeLock;
            DotnetLock = dotnetLock;
        }

        public ProcessLock NodeLock { get; }
        public ProcessLock DotnetLock { get; }

        /// <summary>
        /// Waits asynchronously to acquire the locks in order.
        /// </summary>
        public async Task WaitAsync()
        {
            if (NodeLock == null)
            {
                await DotnetLock.WaitAsync();
                _dotNetLockTaken = true;
                return;
            }

            try
            {
                // We want to take the NPM lock first as is going to be the busiest one, and we want other threads to be
                // able to run dotnet new while we are waiting for another thread to finish running NPM.
                await NodeLock.WaitAsync();
                _nodeLockTaken = true;
                await DotnetLock.WaitAsync();
                _dotNetLockTaken = true;
            }
            catch
            {
                if (_nodeLockTaken)
                {
                    NodeLock.Release();
                    _nodeLockTaken = false;
                }

                if(_dotNetLockTaken)
                {
                    DotnetLock.Release();
                    _dotNetLockTaken = false;
                }
                throw;
            }
        }
    }

    /// <summary>
    /// Captures the binary log on failure.
    /// </summary>
    /// <param name="result">The process result.</param>
    private void CaptureBinLogOnFailure(ProcessEx result)
    {
        if (result.ExitCode != 0 && !string.IsNullOrEmpty(ArtifactsLogDir))
        {
            Assert.NotNull(TemplateOutputDir);

            var sourceFile = Path.Combine(TemplateOutputDir, "msbuild.binlog");
            Assert.True(File.Exists(sourceFile), $"Log for '{ProjectName}' not found in '{sourceFile}'. Execution output: {result.Output}");
            
            if(!Directory.Exists(ArtifactsLogDir))
            {
                Directory.CreateDirectory(ArtifactsLogDir);
            }

            var destination = Path.Combine(ArtifactsLogDir, ProjectName + ".binlog");
            File.Move(sourceFile, destination, overwrite: true); // binlog will exist on retries
        }
    }

    public override string ToString() => $"{ProjectName}: {TemplateOutputDir}";

    /// <summary>
    /// Gets the assembly metadata for a specified key.
    /// </summary>
    /// <param name="key">The key of the metadata to retrieve.</param>
    /// <returns>The value of the metadata.</returns>
    private static string GetAssemblyMetadata(string key)
    {
        var attribute = typeof(Project).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key);

        if (attribute is null)
        {
            throw new ArgumentException($"AssemblyMetadataAttribute with key {key} was not found.");
        }

        Assert.NotNull(attribute.Value);
        return attribute.Value;
    }
}
