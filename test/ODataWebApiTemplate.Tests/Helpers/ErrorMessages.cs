//---------------------------------------------------------------------
// <copyright file="ErrorMessages.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace ODataWebApiTemplate.Tests.Helpers;

internal static class ErrorMessages
{
    public static string GetFailedProcessMessage(string step, Project project, ProcessResult processResult)
    {
        return $@"Project {project.ProjectArguments} failed to {step}. Exit code {processResult.ExitCode}.
{processResult.Process}\nStdErr: {processResult.Error}\nStdOut: {processResult.Output}";
    }

    public static string GetFailedProcessMessageOrEmpty(string step, Project project, ProcessEx process)
    {
        return process.HasExited ? 
            $@"Project {project.ProjectArguments} failed to {step}.{process.GetFormattedOutput()}" 
            : "";
    }
}
