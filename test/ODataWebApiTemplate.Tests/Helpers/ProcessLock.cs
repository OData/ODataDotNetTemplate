//---------------------------------------------------------------------
// <copyright file="ProcessLock.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Represents a lock for synchronizing access to processes, ensuring that only one process can run at a time.
/// </summary>
public class ProcessLock
{
    /// <summary>
    /// A lock for synchronizing access to the 'dotnet new' process.
    /// </summary>
    public static readonly ProcessLock DotNetNewLock = new ProcessLock("dotnet-new");

    /// <summary>
    /// A lock for synchronizing access to the 'node' process.
    /// </summary>
    public static readonly ProcessLock NodeLock = new ProcessLock("node");

    public ProcessLock(string name)
    {
        Name = name;
        Semaphore = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Gets the name of the process lock.
    /// </summary>
    public string Name { get; }
    private SemaphoreSlim Semaphore { get; }

    /// <summary>
    /// Waits asynchronously to acquire the process lock, with an optional timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration to wait for the lock. If not specified, defaults to 20 minutes.</param>
    public async Task WaitAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromMinutes(20);
        Assert.True(await Semaphore.WaitAsync(timeout.Value), $"Unable to acquire process lock for process {Name}");
    }

    /// <summary>
    /// Releases the process lock.
    /// </summary>
    public void Release()
    {
        Semaphore.Release();
    }
}
