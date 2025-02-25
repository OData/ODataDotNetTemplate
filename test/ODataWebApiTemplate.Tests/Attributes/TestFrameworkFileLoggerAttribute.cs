//---------------------------------------------------------------------
// <copyright file="TestFrameworkFileLoggerAttribute.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace ODataWebApiTemplate.Tests.Attributes;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class TestFrameworkFileLoggerAttribute : TestOutputDirectoryAttribute
{
    public TestFrameworkFileLoggerAttribute(string preserveExistingLogsInOutput, string tfm, string? baseDirectory = null)
        : base(preserveExistingLogsInOutput, tfm, baseDirectory)
    {
    }
}
