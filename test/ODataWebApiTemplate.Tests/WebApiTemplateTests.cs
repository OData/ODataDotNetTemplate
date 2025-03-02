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

    private const string Framework = "net6.0";

    #region Tests generating an ASP.NET Core OData Web API project with default options and verifies its functionality.

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GenerateAspNetCoreODataWebApi_With_DefaultOptions(bool enableOpenApiOrSwagger)
    {
        // Arrange
        var args = new[] { $"--enable-openapi {enableOpenApiOrSwagger}" };
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, _logger))
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

            // Swagger
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

    [Fact]
    public async Task GenerateAspNetCoreODataWebApi_TestPost()
    {
        // Arrange
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi");
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, _logger))
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

    [Fact]
    public async Task GenerateAspNetCoreODataWebApi_TestDataPatch()
    {
        // Arrange
        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi");
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, _logger))
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

    [Fact]
    public async Task GenerateAspNetCoreODataWebApi_With_ODataBatchingEnabled()
    {
        // Arrange
        var args = new[] { "--enable-batching True" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        await project.VerifyHasProperty("TargetFramework", Framework);

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, _logger))
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
    [InlineData(true, "expand=orders")]
    [InlineData(true, "expand=Orders")]
    [InlineData(false, "expand=Orders")]
    [InlineData(true, "expand=Orders(Select=amount)")]
    [InlineData(true, "expand=orders(Select=Amount)")]
    [InlineData(false, "Expand=Orders(Select=Amount)")]
    [InlineData(true, "orderBy=name desc")]
    [InlineData(true, "orderBy=Name desc")]
    [InlineData(false, "orderBy=Name desc")]
    [InlineData(true, "filter=type eq 'Premium'")]
    [InlineData(true, "filter=Type eq 'Premium'")]
    [InlineData(false, "filter=Type eq 'Premium'")]
    public async Task GenerateAspNetCoreODataWebApi_With_CaseInsensitive(bool caseInsensitive, string query)
    {
        // Arrange
        var args = new[] { $"--case-insensitive {caseInsensitive}" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, _logger))
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
    [InlineData(true, "expand=Orders")]
    [InlineData(false, "$expand=Orders")]
    [InlineData(true, "expand=Orders&select=Orders")]
    [InlineData(false, "$expand=Orders&$select=Orders")]
    [InlineData(true, "orderBy=Name desc")]
    [InlineData(false, "$orderBy=Name desc")]
    public async Task GenerateAspNetCoreODataWebApi_WithOrWithoutDollarOnQueryOptions(bool withOrWithoutDollar, string query)
    {
        // Arrange
        var args = new[] { $"--no-dollar {withOrWithoutDollar}" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http", "https" });
        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, _logger))
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
    [InlineData("expand", "$expand=Orders", "$filter=Type eq 'Premium'")]
    [InlineData("expand", "$expand=Orders", "$orderBy=Name desc")]
    [InlineData("expand", "$expand=Orders", "$count=true" )]
    [InlineData("expand", "$expand=Orders", "odata/$batch" )]
    [InlineData("filter", "$filter=Type eq 'Premium'", "$expand=Orders" )]
    [InlineData("filter", "$filter=Type eq 'Premium'", "$orderBy=Name desc" )]
    [InlineData("filter", "$filter=Type eq 'Premium'", "$count=true" )]
    [InlineData("expand select", "$expand=Orders($select=Amount)", "$count=true" )]
    [InlineData("expand select", "$expand=Orders($select=Amount)", "$orderBy=Name desc")]
    [InlineData("expand select", "$expand=Orders($select=Amount)", "$filter=Type eq 'Premium'")]
    [InlineData("expand select", "$expand=Orders&$select=Orders", "$filter=Type eq 'Premium'")]
    [InlineData("expand select", "$expand=Orders&$select=Orders", "$orderBy=Name desc")]
    [InlineData("orderby", "$orderBy=Name desc", "$expand=Orders")]
    [InlineData("orderby", "$orderBy=Type", "$filter=Type eq 'Premium'" )]
    [InlineData("orderby", "$orderBy=Name desc", "$Count=true")]
    [InlineData("count", "$count=true", "$orderBy=Name desc")]
    [InlineData("count", "$count=true", "$expand=Orders($select=Amount)")]
    [InlineData("count", "$count=true", "$filter=Type eq 'Premium'")]
    [InlineData("expand filter count orderby select", "$count=true&$expand=Orders&$filter=Type eq 'Premium'&$select=Type", "odata/$batch")]

    public async Task GenerateAspNetCoreODataWebApi_With_SelectedQueryOptions(string queryOptions, string query, string notFoundOrBadRequestQuery)
    {
        // Arrange
        var args = new[] { $"--query-option {queryOptions}", "--configurehttps False" };

        var project = await FactoryFixture.CreateProject(Output);

        // Act & Assert
        await project.RunDotNetNewAsync("odata-webapi", args: args);
        await project.VerifyLaunchSettings(new[] { "http" });
        await project.RunDotNetBuildAsync();

        await project.VerifyHasProperty("TargetFramework", Framework);

        using (var aspNetProcess = project.StartBuiltProjectAsync(true, _logger))
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
}
