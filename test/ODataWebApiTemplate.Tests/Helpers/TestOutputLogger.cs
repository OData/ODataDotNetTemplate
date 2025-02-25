//---------------------------------------------------------------------
// <copyright file="TestOutputLogger.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ODataWebApiTemplate.Tests.Helpers;

internal sealed class TestOutputLogger : ITestOutputHelper
{
    private readonly ILogger _logger;

    public TestOutputLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void WriteLine(string message)
    {
        _logger.LogInformation(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, format, args));
        }
    }
}
