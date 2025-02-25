//---------------------------------------------------------------------
// <copyright file="TestOutputDirectoryAttribute.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace ODataWebApiTemplate.Tests.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
public class TestOutputDirectoryAttribute : Attribute
{
    public TestOutputDirectoryAttribute(string preserveExistingLogsInOutput, string targetFramework, string? baseDirectory = null)
    {
        TargetFramework = targetFramework;
        BaseDirectory = baseDirectory;
        PreserveExistingLogsInOutput = bool.Parse(preserveExistingLogsInOutput);
    }

    public string? BaseDirectory { get; }
    public string? TargetFramework { get; }
    public bool PreserveExistingLogsInOutput { get; }
}
