//---------------------------------------------------------------------
// <copyright file="WebApiTemplateTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using ODataWebApiTemplate.Tests.Helpers;
using Xunit.Abstractions;

namespace ODataWebApiTemplate.Tests;

/// <summary>
/// Contains tests for generating and verifying ASP.NET Core OData Web API projects using various configurations and options.
/// </summary>
public class WebApiTemplateTests : IClassFixture<ProjectFactoryFixture>
{
    private const string WebApiTemplateTestName = nameof(WebApiTemplateTests);
    private readonly ILogger _logger;
    public WebApiTemplateTests(ProjectFactoryFixture factoryFixture)
    {
        FactoryFixture = factoryFixture;
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = factory.CreateLogger(WebApiTemplateTestName);
    }

    public ProjectFactoryFixture FactoryFixture { get; }

    private ITestOutputHelper? _output;
    public ITestOutputHelper Output
    {
        get
        {
            _output ??= new TestOutputLogger(_logger);
            return _output;
        }
    }

    #region Tests generating an ASP.NET Core OData Web API project with default options and verifies its functionality.

    [Theory]
    [InlineData(true, "net9.0")]
    [InlineData(false, "net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_With_DefaultOptions_VerifyOpenAPIAndSwagger(bool enableOpenApiOrSwagger, string framework)
    {
        // Arrange
        var args = new[] { $"--enable-openapi {enableOpenApiOrSwagger}", $"-f {framework}" };
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");
            await aspNetProcess.AssertOk("odata/Customers/GetCustomerByName(name='Customer1')");
            await aspNetProcess.AssertOk("odata/Customers(1)/GetCustomerOrdersTotalAmount");
            await aspNetProcess.AssertOk("odata/Customers?$Expand=Orders");
            await aspNetProcess.AssertOk("odata/customers?$filter=Type eq 'Premium'");
            await aspNetProcess.AssertOk("odata/Customers?$Expand=Orders($Select=Amount)");
            await aspNetProcess.AssertOk("odata/Customers?$Expand=Orders&$Select=Orders");
            await aspNetProcess.AssertOk("odata/Customers?$OrderBy=Name desc");

            // BATCH
            await aspNetProcess.AssertNotFound("odata/$batch");

            // openapi/v1.json
            if (enableOpenApiOrSwagger)
            {
                await aspNetProcess.AssertOk("openapi/v1.json");
                await aspNetProcess.AssertOk("swagger");
            }
            else
            {
                await aspNetProcess.AssertNotFound("openapi/v1.json");
                await aspNetProcess.AssertNotFound("swagger");
            }
        }
    }

    [Theory]
    [InlineData(true, "net6.0")]
    [InlineData(true, "net8.0")]
    [InlineData(false, "net6.0")]
    [InlineData(false, "net8.0")]
    public async Task GenerateAspNetCoreODataWebApi_With_DefaultOptions_VerifySwagger(bool enableOpenApiOrSwagger, string framework)
    {
        // Arrange
        var args = new[] { $"--enable-openapi {enableOpenApiOrSwagger}", $"-f {framework}" };
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");
            await aspNetProcess.AssertOk("odata/Customers/GetCustomerByName(name='Customer1')");
            await aspNetProcess.AssertOk("odata/Customers(1)/GetCustomerOrdersTotalAmount");
            await aspNetProcess.AssertOk("odata/Customers?$Expand=Orders");
            await aspNetProcess.AssertOk("odata/customers?$filter=Type eq 'Premium'");
            await aspNetProcess.AssertOk("odata/Customers?$Expand=Orders($Select=Amount)");
            await aspNetProcess.AssertOk("odata/Customers?$Expand=Orders&$Select=Orders");
            await aspNetProcess.AssertOk("odata/Customers?$OrderBy=Name desc");

            // BATCH
            await aspNetProcess.AssertNotFound("odata/$batch");

            // swagger
            if (enableOpenApiOrSwagger)
            {
                await aspNetProcess.AssertOk("swagger");
            }
            else
            {
                await aspNetProcess.AssertNotFound("swagger");
            }
        }
    }

    #endregion

    #region Tests generating an ASP.NET Core OData Web API project with default options and verifies POST requests.

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_TestPost(string framework)
    {
        // Arrange
        var args = new[] { $"-f {framework}" };
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");

            // POST
            await aspNetProcess.AssertStatusCodeForPostRequest("odata/Customers", @"
            {
                ""Name"": ""JohnDoe1"",
                ""Type"": ""VIP""
            }");
        }
    }

    #endregion

    #region Tests generating an ASP.NET Core OData Web API project with default options and verifies PATCH requests.

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_TestDataPatch(string framework)
    {
        // Arrange
        var args = new[] { $"-f {framework}" };
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");

            // PATCH
            await aspNetProcess.AssertStatusCodeForPatchRequest("odata/Customers(3)", @"
            {
                ""Name"": ""some_username"",
                ""Type"": ""Premium,VIP""
            }");
        }
    }

    #endregion

    #region Tests generating an ASP.NET Core OData Web API project with OData batching enabled and verifies its functionality.

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_With_ODataBatchingEnabled(string framework)
    {
        // Arrange
        var args = new[] { "--enable-batching True", $"-f {framework}" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        await project.VerifyHasProperty("TargetFramework", framework);

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");

            // BATCH
            await aspNetProcess.AssertStatusCodeForODataBatching("odata/$batch", $@"
{{
  ""requests"": [
        {{
            ""id"": ""{Guid.NewGuid()}"",
            ""method"": ""GET"",
            ""url"": ""Customers"",
            ""headers"": {{
              ""content-type"": ""application/json""
            }}
        }},
        {{
            ""id"": ""{Guid.NewGuid()}"",
            ""method"": ""POST"",
            ""url"": ""Customers"",
            ""headers"": {{
              ""content-type"": ""application/json""
            }},
            ""body"": {{
              ""Name"": ""Customer Batch"",
              ""Type"": ""Premium""
            }}
        }},
        {{
            ""id"": ""{Guid.NewGuid()}"",
            ""method"": ""PATCH"",
            ""url"": ""Customers(2)"",
            ""headers"": {{
              ""content-type"": ""application/json""
            }},
            ""body"": {{
              ""Name"": ""Customer Update with Batch"",
              ""Type"": ""Premium,VIP""
            }}
        }},
        {{
            ""id"": ""{Guid.NewGuid()}"",
            ""method"": ""GET"",
            ""url"": ""Customers"",
            ""headers"": {{
              ""content-type"": ""application/json""
            }}
        }}
    ]
}}");
        }
    }

    #endregion

    #region Tests generating an ASP.NET Core OData Web API project with case-insensitive options enabled, and verifies its functionality.

    [Theory]
    [InlineData(true, "expand=orders", "net6.0")]
    [InlineData(true, "expand=Orders", "net8.0")]
    [InlineData(false, "expand=Orders", "net9.0")]
    [InlineData(true, "expand=Orders(Select=amount)", "net6.0")]
    [InlineData(true, "expand=orders(Select=Amount)", "net8.0")]
    [InlineData(false, "Expand=Orders(Select=Amount)", "net9.0")]
    [InlineData(true, "orderBy=name desc", "net6.0")]
    [InlineData(true, "orderBy=Name desc", "net8.0")]
    [InlineData(false, "orderBy=Name desc", "net9.0")]
    [InlineData(true, "filter=type eq 'Premium'", "net6.0")]
    [InlineData(true, "filter=Type eq 'Premium'", "net8.0")]
    [InlineData(false, "filter=Type eq 'Premium'", "net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_With_CaseInsensitive(bool caseInsensitive, string query, string framework)
    {
        // Arrange
        var args = new[] { $"--case-insensitive {caseInsensitive}", $"-f {framework}" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");
            await aspNetProcess.AssertOk($"odata/Customers?{query}");
        }
    }

    #endregion

    #region Tests generating an ASP.NET Core OData Web API project with or without dollar sign on query options and verifies its functionality.

    [Theory]
    [InlineData(true, "expand=Orders", "net6.0")]
    [InlineData(false, "$expand=Orders", "net8.0")]
    [InlineData(true, "expand=Orders&select=Orders", "net9.0")]
    [InlineData(false, "$expand=Orders&$select=Orders", "net6.0")]
    [InlineData(true, "orderBy=Name desc", "net8.0")]
    [InlineData(false, "$orderBy=Name desc", "net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_WithOrWithoutDollarOnQueryOptions(bool withOrWithoutDollar, string query, string framework)
    {
        // Arrange
        var args = new[] { $"--no-dollar {withOrWithoutDollar}", $"-f {framework}" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");
            await aspNetProcess.AssertOk($"odata/Customers?{query}");
        }
    }

    #endregion

    #region Tests generating an ASP.NET Core OData Web API project with selected query options and verifies its functionality.

    [Theory]
    [InlineData("expand", "$expand=Orders", "$filter=Type eq 'Premium'", "net6.0")]
    [InlineData("expand", "$expand=Orders", "$orderBy=Name desc", "net8.0")]
    [InlineData("expand", "$expand=Orders", "$count=true", "net9.0")]
    [InlineData("expand", "$expand=Orders", "odata/$batch", "net6.0")]
    [InlineData("filter", "$filter=Type eq 'Premium'", "$expand=Orders", "net8.0")]
    [InlineData("filter", "$filter=Type eq 'Premium'", "$orderBy=Name desc", "net9.0")]
    [InlineData("filter", "$filter=Type eq 'Premium'", "$count=true", "net6.0")]
    [InlineData("expand select", "$expand=Orders($select=Amount)", "$count=true", "net8.0")]
    [InlineData("expand select", "$expand=Orders($select=Amount)", "$orderBy=Name desc", "net9.0")]
    [InlineData("expand select", "$expand=Orders($select=Amount)", "$filter=Type eq 'Premium'", "net6.0")]
    [InlineData("expand select", "$expand=Orders&$select=Orders", "$filter=Type eq 'Premium'", "net8.0")]
    [InlineData("expand select", "$expand=Orders&$select=Orders", "$filter=Type eq 'Premium'", "net9.0")]
    [InlineData("expand select", "$expand=Orders&$select=Orders", "$orderBy=Name desc", "net9.0")]
    [InlineData("orderby", "$orderBy=Name desc", "$expand=Orders", "net6.0")]
    [InlineData("orderby", "$orderBy=Type", "$filter=Type eq 'Premium'", "net8.0")]
    [InlineData("orderby", "$orderBy=Name desc", "$Count=true", "net9.0")]
    [InlineData("count", "$count=true", "$orderBy=Name desc", "net6.0")]
    [InlineData("count", "$count=true", "$expand=Orders($select=Amount)", "net8.0")]
    [InlineData("count", "$count=true", "$filter=Type eq 'Premium'", "net9.0")]
    [InlineData("expand filter count orderby select", "$count=true&$expand=Orders&$filter=Type eq 'Premium'&$select=Type", "odata/$batch", "net6.0")]
    [InlineData("expand filter count orderby select", "$count=true&$expand=Orders&$filter=Type eq 'Premium'&$select=Type", "odata/$batch", "net8.0")]
    [InlineData("expand filter count orderby select", "$count=true&$expand=Orders&$filter=Type eq 'Premium'&$select=Type", "odata/$batch", "net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_With_SelectedQueryOptions(string queryOptions, string query, string notFoundOrBadRequestQuery, string framework)
    {
        // Arrange
        var args = new[] { $"--query-option {queryOptions}", "--configurehttps False", $"-f {framework}" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http" });
        await project.RunDotNetBuildAsync();

        await project.VerifyHasProperty("TargetFramework", framework);

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");
            await aspNetProcess.AssertOk("odata/Customers?" + query);

            if (notFoundOrBadRequestQuery == "odata/$batch")
            {
                await aspNetProcess.AssertNotFound(notFoundOrBadRequestQuery);
            }
            else
            {
                await aspNetProcess.AssertBadRequest("odata/Customers?" + notFoundOrBadRequestQuery);
            }
        }
    }

    #endregion

    #region  Tests generating an ASP.NET Core OData Web API project with or without Program.Main and verifies its functionality.

    [Theory]
    [InlineData(true, "net6.0")]
    [InlineData(true, "net8.0")]
    [InlineData(true, "net9.0")]
    [InlineData(false, "net6.0")]
    [InlineData(false, "net8.0")]
    [InlineData(false, "net9.0")]
    public async Task GenerateAspNetCoreODataWebApi_With_UseProgramMainOrNot(bool useProgramMain, string framework)
    {
        // Arrange
        var args = new[] { $"--use-program-main {useProgramMain}", $"-f {framework}" };
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();
        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            // GET
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");
        }
    }

    [Theory]
    [InlineData(true, "net6.0", false)]
    [InlineData(true, "net8.0", false)]
    [InlineData(true, "net9.0", true)]
    [InlineData(false, "net6.0", false)]
    [InlineData(false, "net8.0", false)]
    [InlineData(false, "net9.0", true)]
    public async Task GenerateAspNetCoreODataWebApi_With_UseProgramMainOrNot_VerifySwaggerOrOpenApi(bool useProgramMain, string framework, bool openApiJsonExists)
    {
        // Arrange
        var args = new[] { $"--use-program-main {useProgramMain}", $"-f {framework}", "--enable-openapi True" };
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();
        using (var aspNetProcess = project.StartBuiltProjectAsync(true, framework, _logger))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            if(openApiJsonExists)
            {
                await aspNetProcess.AssertOk("openapi/v1.json");
            }
            
            await aspNetProcess.AssertOk("swagger");
            await aspNetProcess.AssertOk("odata");
            await aspNetProcess.AssertOk("odata/$metadata");
        }
    }

    #endregion
}
