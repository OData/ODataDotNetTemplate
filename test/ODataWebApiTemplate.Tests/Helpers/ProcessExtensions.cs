//---------------------------------------------------------------------
// <copyright file="ArgumentNullThrowHelper.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Provides extension methods for the <see cref="Process"/> class to handle process termination, including terminating child processes.
/// </summary>
internal static class ProcessExtensions
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Kills the process and all its child processes using the default timeout.
    /// </summary>
    /// <param name="process">The process to be terminated.</param>
    public static void KillTree(this Process process) => process.KillTree(_defaultTimeout);

    /// <summary>
    /// Kills the process and all its child processes using the specified timeout.
    /// </summary>
    /// <param name="process">The process to be terminated.</param>
    /// <param name="timeout">The timeout duration to wait for the process termination.</param>
    public static void KillTree(this Process process, TimeSpan timeout)
    {
        var pid = process.Id;
        if (OperatingSystem.IsWindows())
        {
            RunProcessAndWaitForExit(
                "taskkill",
                $"/T /F /PID {pid}",
                timeout,
                out var _);
        }
        else
        {
            var children = new HashSet<int>();
            GetAllChildIdsUnix(pid, children, timeout);
            foreach (var childId in children)
            {
                KillProcessUnix(childId, timeout);
            }
            KillProcessUnix(pid, timeout);
        }
    }

    /// <summary>
    /// Recursively retrieves all child process IDs for a given parent process ID on Unix-based systems.
    /// </summary>
    /// <param name="parentId">The parent process ID.</param>
    /// <param name="children">A set to store the child process IDs.</param>
    /// <param name="timeout">The timeout duration to wait for the process retrieval.</param>
    private static void GetAllChildIdsUnix(int parentId, ISet<int> children, TimeSpan timeout)
    {
        try
        {
            RunProcessAndWaitForExit(
                "pgrep",
                $"-P {parentId}",
                timeout,
                out var stdout);

            if (!string.IsNullOrEmpty(stdout))
            {
                using (var reader = new StringReader(stdout))
                {
                    while (true)
                    {
                        var text = reader.ReadLine();
                        if (text == null)
                        {
                            return;
                        }

                        if (int.TryParse(text, out var id))
                        {
                            children.Add(id);
                            // Recursively get the children
                            GetAllChildIdsUnix(id, children, timeout);
                        }
                    }
                }
            }
        }
        catch (Win32Exception ex) when (ex.Message.Contains("No such file or directory"))
        {
            // This probably means that pgrep isn't installed. Nothing to be done?
        }
    }

    /// <summary>
    /// Kills a process on Unix-based systems using the specified process ID and timeout.
    /// </summary>
    /// <param name="processId">The process ID to be terminated.</param>
    /// <param name="timeout">The timeout duration to wait for the process termination.</param>
    private static void KillProcessUnix(int processId, TimeSpan timeout)
    {
        try
        {
            RunProcessAndWaitForExit(
                "kill",
                $"-TERM {processId}",
                timeout,
                out var stdout);
        }
        catch (Win32Exception ex) when (ex.Message.Contains("No such file or directory"))
        {
            // This probably means that the process is already dead
        }
    }

    /// <summary>
    /// Runs a process with the specified file name and arguments, waits for its exit, and captures the standard output.
    /// </summary>
    /// <param name="fileName">The name of the executable file to run.</param>
    /// <param name="arguments">The arguments to pass to the executable file.</param>
    /// <param name="timeout">The timeout duration to wait for the process exit.</param>
    /// <param name="stdout">The standard output of the process.</param>
    private static void RunProcessAndWaitForExit(string fileName, string arguments, TimeSpan timeout, out string? stdout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        var process = Process.Start(startInfo);
        Assert.NotNull(process);

        stdout = null;
        if (process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            stdout = process.StandardOutput.ReadToEnd();
        }
        else
        {
            process.Kill();
        }
    }
}
