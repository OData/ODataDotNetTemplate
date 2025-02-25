//---------------------------------------------------------------------
// <copyright file="RetryHelper.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System.Net;
using Microsoft.Extensions.Logging;

namespace ODataWebApiTemplate.Tests.Helpers;

/// <summary>
/// Provides helper methods for retrying operations and HTTP requests with configurable retry logic.
/// </summary>
public class RetryHelper
{
    /// <summary>
    /// Retries an HTTP request every 1 second for a specified number of times, defaulting to 60 retries.
    /// </summary>
    /// <param name="retryBlock">The function that performs the HTTP request.</param>
    /// <param name="logger">The logger to log retry attempts and errors.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the retry operation.</param>
    /// <param name="retryCount">The number of times to retry the request. Defaults to 60.</param>
    /// <returns>The HTTP response message if the request is successful.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled or the retry limit is exceeded.</exception>
    public static async Task<HttpResponseMessage> RetryRequest(
        Func<Task<HttpResponseMessage>> retryBlock,
        ILogger logger,
        CancellationToken cancellationToken = default,
        int retryCount = 60)
    {
        for (var retry = 0; retry < retryCount; retry++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Failed to connect, retry canceled.");
                throw new OperationCanceledException("Failed to connect, retry canceled.", cancellationToken);
            }

            try
            {
                logger.LogWarning("Retry count {retryCount}..", retry + 1);
                var response = await retryBlock().ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    // Automatically retry on 503. May be application is still booting.
                    logger.LogWarning("Retrying a service unavailable error.");
                    continue;
                }

                return response; // Went through successfully
            }
            catch (Exception exception)
            {
                if (retry == retryCount - 1)
                {
                    logger.LogError(0, exception, "Failed to connect, retry limit exceeded.");
                    throw;
                }
                else
                {
                    if (exception is HttpRequestException || exception is WebException)
                    {
                        logger.LogWarning("Failed to complete the request : {0}.", exception.Message);
                        await Task.Delay(1 * 1000); //Wait for a while before retry.
                    }
                }
            }
        }

        logger.LogInformation("Failed to connect, retry limit exceeded.");
        throw new OperationCanceledException("Failed to connect, retry limit exceeded.");
    }

    /// <summary>
    /// Retries an operation a specified number of times with an optional delay between retries.
    /// </summary>
    /// <param name="retryBlock">The action to retry.</param>
    /// <param name="exceptionBlock">The action to execute if an exception occurs.</param>
    /// <param name="retryCount">The number of times to retry the operation. Defaults to 3.</param>
    /// <param name="retryDelayMilliseconds">The delay in milliseconds between retries. Defaults to 0.</param>
    public static void RetryOperation(
        Action retryBlock,
        Action<Exception> exceptionBlock,
        int retryCount = 3,
        int retryDelayMilliseconds = 0)
    {
        for (var retry = 0; retry < retryCount; ++retry)
        {
            try
            {
                retryBlock();
                break;
            }
            catch (Exception exception)
            {
                exceptionBlock(exception);
            }

            Thread.Sleep(retryDelayMilliseconds);
        }
    }
}