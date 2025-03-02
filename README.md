# ASP.NET Core OData Sample Template

This repository provides a .NET template for creating an ASP.NET Core WebAPI project with OData support. It supports configurations for .NET 6.0 and above, with appropriate setups for each version.

## Prerequisites

- [Download and install .NET](https://dotnet.microsoft.com/en-us/download)
- [Visual Studio IDE](https://visualstudio.microsoft.com/#vs-section) - optional
- [VS Code](https://visualstudio.microsoft.com/#vscode-section) - optional

## Getting Started

Follow these steps to use the template locally:

### 1. Clone the Repository

```powershell
git clone https://github.com/OData/AspNetCoreODataDotNetTemplate.git
```

### 2. Project Build and Content Generation

This project uses MSBuild to automate the build process and generate content from templates. Below are key files involved in this process.

#### Files

- **Directory.Build.targets**: [`tools/Directory.Build.targets`](./tools/Directory.Build.targets) contains custom MSBuild targets applied to all projects in the directory and its subdirectories.
- **GenerateContent.targets**: [`tools/GenerateContent.targets`](./tools/GenerateContent.targets) defines targets for generating content based on templates.
- **GenerateFileFromTemplate.cs**: [`tools/GenerateFileFromTemplate.cs`](./tools/GenerateFileFromTemplate.cs) implements the `GenerateFileFromTemplate` task. It is responsible for reading template files, applying the specified properties, and generating the output files.
- **GenerateTemplate.tasks.targets**: [`tools/GenerateTemplate.tasks.targets`](./tools/GenerateTemplate.tasks.targets) imports the `GenerateFileFromTemplate` task and defines necessary targets. It ensures that the task is available for use in the `GenerateContent.targets` file.
- **Packages.targets**: [`tools/Packages.targets`](./tools/Packages.targets) defines packages used with the template.
- **Versions.targets**: [`tools/Versions.targets`](./tools/Versions.targets) contains targets for managing version information.
- **Directory.Build.props**: [`Directory.Build.props`](./Directory.Build.props) contains common properties applied to all projects in the directory and its subdirectories.

### 3. Build Repo

Navigate to the cloned repository directory and build the project to restore necessary packages and dependencies:

```powershell
cd <repository-directory>/AspNetCoreODataDotNetTemplate/sln
dotnet build
```

### 4. Use build.cmd/build.ps1 Script

At the root, there is a PowerShell script (`build.ps1`) that automates building, creating NuGet packages, and testing the AspNetCoreOData template project.

#### Usage

To run the script, open a PowerShell terminal, navigate to the directory containing the `build.cmd` file, and execute the script with the `-help` parameter:

```powershell
build.cmd -help
```

#### Examples

1. **Build the solution with default settings:**
  ```powershell
  .\build.cmd
  ```

2. **Build the solution in Debug configuration and run tests with detailed verbosity:**
  ```powershell
  .\build.cmd -SolutionPath ".\sln\MySolution.sln" -c "Debug" -Test -v "Detailed"
  ```

3. **Build the solution and create NuGet packages:**
  ```powershell
  .\build.cmd -SolutionPath ".\sln\MySolution.sln"
  ```

4. **Running tests:**
  ```powershell
  .\build.cmd -Test
  ```

## Artifacts

Building this repo produces artifacts in the following structure:

```text
artifacts/
  bin/                 = Compiled binaries and executables
  obj/                 = Intermediate object files and build logs
  log/
    *.log            = Log files for test runs and individual tests
  $(Configuration)/
    *.binlog         = Binary logs for most build phases
  packages/
  $(Configuration)/
    *.nupkg        = NuGet packages for nuget.org
```

## Branch Strategy

- `main` contains actively developed code not yet released.
- `release/<target-dotnet-framework>` contains code intended for release, targeting specific .NET framework versions.

## Template Project Structure

The generated project will have the following structure:

```css
ODataWebApiApplication/
├── Controllers/
│   └── CustomersController.cs
├── Models/
│   └── Customer.cs
│   └── Order.cs
├── Properties/
│   └── launchSettings.json
├── EdmModelBuilder.cs
├── ODataWebApiApplication.csproj
├── ODataWebApiApplication.http
├── Program.cs
└── appsettings.Development.json
└── appsettings.json
```
