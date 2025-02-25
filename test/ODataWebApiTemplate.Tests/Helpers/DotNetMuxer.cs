//---------------------------------------------------------------------
// <copyright file="DotNetMuxer.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Utilities for finding the "dotnet.exe" file from the currently running .NET Core application
/// </summary>
internal static class DotNetMuxer
{
    private const string MuxerName = "dotnet";

    static DotNetMuxer()
    {
        MuxerPath = TryFindMuxerPath(Process.GetCurrentProcess().MainModule?.FileName);
    }

    /// <summary>
    /// The full filepath to the .NET Core muxer.
    /// </summary>
    public static string? MuxerPath { get; }

    /// <summary>
    /// Finds the full filepath to the .NET Core muxer,
    /// or returns a string containing the default name of the .NET Core muxer ('dotnet').
    /// </summary>
    /// <returns>The path or a string named 'dotnet'.</returns>
    public static string MuxerPathOrDefault() => MuxerPath ?? MuxerName;

    internal static string? TryFindMuxerPath(string? mainModule)
    {
        var fileName = MuxerName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName += ".exe";
        }

        if (!string.IsNullOrEmpty(mainModule)
            && string.Equals(Path.GetFileName(mainModule!), fileName, StringComparison.OrdinalIgnoreCase))
        {
            return mainModule;
        }

        return null;
    }
}
