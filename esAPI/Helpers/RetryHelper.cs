namespace esAPI.Helpers;

public static class RetryHelper
{
    /// <summary>
    /// Retries an async function a specified number of times with a delay between attempts.
    /// </summary>
    /// <typeparam name="T">The return type of the function to retry.</typeparam>
    /// <param name="action">The async function to execute.</param>
    /// <param name="retryCount">The total number of attempts (e.g., 3 means 1 initial try + 2 retries).</param>
    /// <param name="delay">The time to wait between failed attempts.</param>
    /// <param name="logger">A logger to record retry attempts.</param>
    /// <returns>The result of the function if successful; otherwise, the default value of T.</returns>
    public static async Task<T?> TryExecuteAsync<T>(
        Func<Task<T?>> action,
        int retryCount,
        TimeSpan delay) where T : class
    {
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                // logger.LogError(ex, "Attempt {AttemptNumber} of {TotalAttempts} failed. Retrying in {Delay}...",
                //     i + 1, retryCount, delay);

                if (i == retryCount - 1)
                {
                    throw;
                }

                await Task.Delay(delay);
            }
        }

        return default;
    }

    public static async Task<bool> TryExecuteAsync(
        Func<Task<bool>> action,
        int retryCount,
        TimeSpan delay)
    {
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                if (await action())
                {
                    return true;
                }

                // logger.LogWarning("Attempt {AttemptNumber} of {TotalAttempts} returned false. Retrying in {Delay}...",
                //     i + 1, retryCount, delay);

                if (i == retryCount - 1)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                // logger.LogError(ex, "Attempt {AttemptNumber} of {TotalAttempts} threw an exception. Retrying in {Delay}...",
                //     i + 1, retryCount, delay);

                if (i == retryCount - 1)
                {
                    throw;
                }
            }

            await Task.Delay(delay);
        }
        return false;
    }
}