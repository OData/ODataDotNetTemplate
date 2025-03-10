# ASP.NET Core OData WebAPI Template

---

Component | Build  | Status 
--------|--------- |---------
ASP.NET Core ODataDotNetTemplate|Rolling | [![Build Status](https://msazure.visualstudio.com/One/_apis/build/status/407212?repoName=OData/ODataDotNetTemplate&branchName=main)](https://msazure.visualstudio.com/One/_build/latest/407212?repoName=OData/ODataDotNetTemplate&branchName=main)
ASP.NET Core ODataDotNetTemplate|Nightly | [![Build Status](https://msazure.visualstudio.com/One/_apis/build/status/407217?repoName=OData/ODataDotNetTemplate&branchName=main)](https://msazure.visualstudio.com/One/_build/latest/407217?repoName=OData/ODataDotNetTemplate&branchName=main)

This repository provides a .NET template for creating an ASP.NET Core WebAPI project with OData support. It supports configurations for .NET 6.0, 8.0 and above, with appropriate setups for each version.

## Prerequisites

- [Download and install .NET](https://dotnet.microsoft.com/download)
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

- **Directory.Build.targets**: [`tools/Directory.Build.targets`](./tools/Directory.Build.targets) contains custom MSBuild targets applied to all projects in the directory and its subdirectories. Also contains targets for managing version information.
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

## IncrementVersion.ps1

This script increments the version number in the specified msbuild props file. The version number can be incremented in the following ways:
- Major version increment
- Minor version increment
- Revision version increment
- Version release number increment

### Parameters

- `versionPath`: The path to the msbuild props file where the version number is specified.
- `lastReleaseCommit`: The ID of the last commit to be released.
- `forceMajorIncrement`: Whether to force an increment of the major version number.
- `versionRelease`: The version number to be released. For example, preview, beta, alpha, etc.
- `forceMinorIncrement`: Whether to force an increment of the minor version number.
- `Help`: Show help.

### Examples

Increment the minor version number in the `Directory.Build.targets` file:

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

## Code of Conduct

This project has adopted the [.NET Foundation Contributor Covenant Code of Conduct](https://dotnetfoundation.org/about/policies/code-of-conduct). For more information see the [Code of Conduct FAQ](https://dotnetfoundation.org/about/faq).

### .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

ODataDotNetTemplate is a Copyright of &copy; .NET Foundation and other contributors. It is licensed under [MIT License](https://github.com/OData/ODataDotNetTemplate/blob/main/License.txt)
