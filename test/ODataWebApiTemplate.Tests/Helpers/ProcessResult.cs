//---------------------------------------------------------------------
// <copyright file="ProcessResult.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
internal sealed class ProcessResult
{
    public ProcessResult(ProcessEx process)
    {
        Process = process.Process.StartInfo.FileName + " " + process.Process.StartInfo.Arguments;
        ExitCode = process.ExitCode;
        Output = process.Output;
        Error = process.Error;
    }

    public string Process { get; }

    public int ExitCode { get; set; }

    public string Error { get; }

    public string Output { get; }
}
