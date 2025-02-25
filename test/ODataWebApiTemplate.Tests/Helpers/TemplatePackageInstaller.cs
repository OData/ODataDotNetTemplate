//---------------------------------------------------------------------
// <copyright file="TemplatePackageInstaller.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Reflection;
using Xunit.Abstractions;

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Provides methods to install and verify template packages for testing purposes.
/// </summary>
internal static class TemplatePackageInstaller
{
    private static bool _haveReinstalledTemplatePackages;

    private static readonly string _templatePackage = "Microsoft.AspNetCoreOData.WebApiTemplate.9.0";

    /// <summary>
    /// Gets the custom template hive path from the assembly metadata.
    /// </summary>
    public static string? CustomTemplateHivePath
    {
        get
        {
            return typeof(TemplatePackageInstaller).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .SingleOrDefault(s => s.Key == "CustomTemplateHivePath")?.Value;
        }
    }

    /// <summary>
    /// Ensures that the templating engine is initialized by reinstalling template packages if necessary.
    /// </summary>
    /// <param name="output">The test output helper for logging output.</param>
    public static async Task EnsureTemplatingEngineInitializedAsync(ITestOutputHelper output)
    {
        if (!_haveReinstalledTemplatePackages)
        {
            if (Directory.Exists(CustomTemplateHivePath))
            {
                Directory.Delete(CustomTemplateHivePath, recursive: true);
            }
            await InstallTemplatePackages(output);
            _haveReinstalledTemplatePackages = true;
        }
    }

    /// <summary>
    /// Runs the 'dotnet new' command with the specified arguments.
    /// </summary>
    /// <param name="output">The test output helper for logging output.</param>
    /// <param name="arguments">The arguments for the 'dotnet new' command.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the process execution result.</returns>
    public static async Task<ProcessEx> RunDotNetNew(ITestOutputHelper output, string arguments)
    {
        var proc = ProcessEx.Run(
            output,
            AppContext.BaseDirectory,
            DotNetMuxer.MuxerPathOrDefault(),
            //--debug:disable-sdk-templates means, don't include C:\Program Files\dotnet\templates, aka. what comes with SDK, so we don't need to uninstall
            //--debug:custom-hive means, don't install templates on CI/developer machine, instead create new temporary instance
            $"new {arguments} --debug:disable-sdk-templates --debug:custom-hive \"{CustomTemplateHivePath}\"");

        await proc.Exited;

        return proc;
    }

    /// <summary>
    /// Installs the template packages required for testing.
    /// </summary>
    /// <param name="output">The test output helper for logging output.</param>
    private static async Task InstallTemplatePackages(ITestOutputHelper output)
    {
        var packagesDir = typeof(TemplatePackageInstaller).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "ArtifactsShippingPackagesDir").Value;
        Assert.NotNull(packagesDir);

        var builtPackages = Directory.EnumerateFiles(packagesDir, "Microsoft.AspNetCoreOData.WebApiTemplate*.nupkg")
            .Where(p => Path.GetFileName(p).StartsWith(_templatePackage, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (builtPackages.Length == 0)
        {
            throw new InvalidOperationException($"Failed to find required templates in {packagesDir}. Please ensure the *Templates*.nupkg have been built.");
        }

        await VerifyCannotFindTemplateAsync(output, "odata-webapi");

        var packagePath = builtPackages.OrderByDescending(p => p).First();
        output.WriteLine($"Installing templates package {packagePath}...");
        var result = await RunDotNetNew(output, $"install \"{packagePath}\"");
        Assert.True(result.ExitCode == 0, result.GetFormattedOutput());

        await VerifyCanFindTemplate(output, "odata-webapi");
    }

    /// <summary>
    /// Verifies that the specified template can be found.
    /// </summary>
    /// <param name="output">The test output helper for logging output.</param>
    /// <param name="templateName">The name of the template to verify.</param>
    private static async Task VerifyCanFindTemplate(ITestOutputHelper output, string templateName)
    {
        var proc = await RunDotNetNew(output, $"--list");
        if (!(proc.Output.Contains($" {templateName} ") || proc.Output.Contains($",{templateName}") || proc.Output.Contains($"{templateName},")))
        {
            throw new InvalidOperationException($"Couldn't find {templateName} as an option in {proc.Output}.");
        }
    }

    /// <summary>
    /// Verifies that the specified template cannot be found.
    /// </summary>
    /// <param name="output">The test output helper for logging output.</param>
    /// <param name="templateName">The name of the template to verify.</param>
    private static async Task VerifyCannotFindTemplateAsync(ITestOutputHelper output, string templateName)
    {
        // Verify we really did remove the previous templates
        var tempDir = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName(), Guid.NewGuid().ToString("D"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var proc = await RunDotNetNew(output, $"\"{templateName}\"");

            if (!proc.Error.Contains("No templates or subcommands found matching:"))
            {
                throw new InvalidOperationException($"Failed to uninstall previous templates. The template '{templateName}' could still be found.");
            }
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
