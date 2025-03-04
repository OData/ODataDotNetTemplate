# OData ASP.NET Core Web Template Pack for Microsoft Template Engine
This .NET template helps you create an ASP.NET Core WebAPI project with OData support. It supports different configurations for .NET 6.0 and above, with appropriate setup for each version.

## Prerequisites

- [Download and install .NET](https://dotnet.microsoft.com/en-us/download)
- [Visual Studio IDE](https://visualstudio.microsoft.com/#vs-section) - optional
- [VS Code](https://visualstudio.microsoft.com/#vscode-section) - optional

## Getting Started

Follow these steps to use the template locally:

### 1. Install the Template

Install the template using the dotnet new command:

```bash
dotnet new install Microsoft.AspNetCoreOData.WebApiTemplate.9.0
```

To specify the PackageVersion explicitly:

```bash
dotnet new install Microsoft.AspNetCoreOData.WebApiTemplate.9.0::<PackageVersion>
```

### 2. Create a New Project Using the Template

You can create a new project using the template with different target frameworks and OData versions. Here are some examples:

#### View Template Options

```bash
dotnet new odata-webapi --help
```

```bash
Usage:
  dotnet new odata-webapi [options] [template options]

Options:
  -n, --name <name>       The name for the output being created. If no name is specified, the name of the output directory is used.
  -o, --output <output>   Location to place the generated output.
  --dry-run               Displays a summary of what would happen if the given command line were run if it would result in a template creation.
  --force                 Forces content to be generated even if it would change existing files.
  --no-update-check       Disables checking for the template package updates when instantiating a template.
  --project <project>     The project that should be used for context evaluation.
  -lang, --language <C#>  Specifies the template language to instantiate.
  --type <project>        Specifies the template type to instantiate.

Template options:
  -f, --framework <net9.0>                                  The target framework for the project.
                                                            Type: choice
                                                              net9.0  Target net9.0
                                                            Default: net9.0
  -qo, --query-option <count|expand|filter|orderby|select>  OData System Query Options.
                                                            Type: choice
                                                              filter   Enable $filter query option
                                                              select   Enable $select query option
                                                              expand   Enable $expand query option
                                                              orderby  Enable $orderby query option
                                                              count    Enable $count query option
                                                            Multiple values are allowed: True
                                                            Default: filter|select|expand|orderby|count
  --no-dollar                                               Whether or not the OData system query options should be prefixed with '$'
                                                            Type: bool
                                                            Default: true
  -ci, --case-insensitive                                   Enable case insensitive for the controller/action property name in conventional routing
                                                            Type: bool
                                                            Default: true
  --enable-batching                                         Allowing OData batching
                                                            Type: bool
                                                            Default: false
  --configurehttps                                          Configure HTTPS
                                                            Type: bool
                                                            Default: true
  --enable-openapi                                          Enable OpenAPI (Swagger) support
                                                            Type: bool
                                                            Default: false
  --use-program-main                                        Whether to generate an explicit Program class and Main method instead of top-level statements.
                                                            Type: bool
                                                            Default: false
  --exclude-launch-settings                                 Whether to exclude launchSettings.json in the generated template.
                                                            Type: bool
                                                            Default: false
  --no-restore                                              If specified, skips the automatic restore of the project on create.
                                                            Type: bool
                                                            Default: false
```

#### Create Template with Default Settings

```bash
dotnet new odata-webapi -n MyODataService.API1 --framework net9.0
```

#### Enable OData Batching and with all Query-Options

```bash
dotnet new odata-webapi -n MyODataService.API2 --configureHttps true --enable-batching true
```

#### Enable a few Query-Options

```bash
dotnet new odata-webapi -n MyODataService.API4 --query-option filter select expand
```

#### Enable OpenAPI/Swagger

```bash
dotnet new odata-webapi -n MyODataService.API5 --framework net9.0 --enable-openapi true
```

### 3. Run the Project

Navigate to the project directory and run the project using the dotnet run command:

```bash
cd MyODataService.API1
dotnet run
```

### 4. Uninstall the Template

Uninstall the template using the dotnet new command:

```bash
dotnet new uninstall Microsoft.AspNetCoreOData.WebApiTemplate.9.0
```

## Project Structure

The generated project will have the following structure:

```css
ODataWebApiApplication/
├── Controllers/
│   └── CustomersController.cs
├── Models/
│   └── Customer.cs
│   └── EdmModelBuilder.cs
│   └── Order.cs
├── Properties/
│   └── launchSettings.json
├── ODataWebApiApplication.csproj
├── ODataWebApiApplication.http
├── Program.cs
└── appsettings.Development.json
└── appsettings.json
```

## Repository

https://github.com/OData/AspNetCoreOData

