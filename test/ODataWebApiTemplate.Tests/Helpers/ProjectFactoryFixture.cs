//---------------------------------------------------------------------
// <copyright file="ProjectFactoryFixture.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Reflection;
using Xunit.Abstractions;

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Provides a fixture for creating and managing test projects, ensuring unique project keys and handling project disposal.
/// </summary>
public class ProjectFactoryFixture : IDisposable
{
    private const string LetterChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private readonly ConcurrentDictionary<string, Project> _projects = new ConcurrentDictionary<string, Project>();

    /// <summary>
    /// Gets the diagnostics message sink for logging diagnostic messages.
    /// </summary>
    public IMessageSink DiagnosticsMessageSink { get; }

    public ProjectFactoryFixture(IMessageSink diagnosticsMessageSink)
    {
        Assert.NotNull(diagnosticsMessageSink);
        DiagnosticsMessageSink = diagnosticsMessageSink;
    }

    /// <summary>
    /// Creates a new project for testing.
    /// </summary>
    /// <param name="output">The test output helper for logging output.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created project.</returns>
    public async Task<Project> CreateProject(ITestOutputHelper output)
    {
        await TemplatePackageInstaller.EnsureTemplatingEngineInitializedAsync(output);

        var project = CreateProjectImpl(output);

        var projectKey = Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant();
        if (!_projects.TryAdd(projectKey, project))
        {
            throw new InvalidOperationException($"Project key collision in {nameof(ProjectFactoryFixture)}.{nameof(CreateProject)}!");
        }

        return project;
    }

    /// <summary>
    /// Creates a new project instance with the specified output helper.
    /// </summary>
    /// <param name="output">The test output helper for logging output.</param>
    /// <returns>The created project instance.</returns>
    private Project CreateProjectImpl(ITestOutputHelper output)
    {
        Assert.NotNull(output);

        var project = new Project
        {
            Output = output,
            DiagnosticsMessageSink = DiagnosticsMessageSink,
            // Ensure first character is a letter to avoid random insertions of '_' into template namespace
            // declarations (i.e. make it more stable for testing)
            ProjectGuid = GetRandomLetter() + Path.GetRandomFileName().Replace(".", string.Empty)
        };

        project.ProjectName = $"ODataWebApiTemplates.{project.ProjectGuid}";

        var assemblyPath = GetType().Assembly;
        var basePath = GetTemplateFolderBasePath(assemblyPath);
        Assert.NotNull(basePath);

        project.TemplateOutputDir = Path.Combine(basePath, project.ProjectName);

        return project;
    }

    private static char GetRandomLetter() => LetterChars[Random.Shared.Next(LetterChars.Length - 1)];

    /// <summary>
    /// Gets the base path for the template folder from the assembly metadata.
    /// </summary>
    /// <param name="assembly">The assembly to get the metadata from.</param>
    /// <returns>The base path for the template folder.</returns>
    private static string? GetTemplateFolderBasePath(Assembly assembly) =>
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "TestTemplateCreationFolder")
            .Value;

    public void Dispose()
    {
        var list = new List<Exception>();
        foreach (var project in _projects)
        {
            try
            {
                project.Value.Dispose();
            }
            catch (Exception e)
            {
                list.Add(e);
            }
        }

        if (list.Count > 0)
        {
            throw new AggregateException(list);
        }
    }
}
