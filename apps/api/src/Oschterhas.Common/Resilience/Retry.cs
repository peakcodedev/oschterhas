namespace Oschterhas.Common.Resilience;

/// <summary>
/// Small retry helper with exponential backoff (replaces Polly, see docs/lizenzen.md).
/// Use it only for transient failures, e.g. waiting for the database at startup.
/// </summary>
public static class Retry
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        RetryOptions options,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxAttempts, 1);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await action(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException
                && attempt < options.MaxAttempts
                && options.ShouldRetry(ex))
            {
                await Task.Delay(options.GetDelay(attempt), timeProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public static Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        RetryOptions options,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ExecuteAsync<bool>(
            async ct =>
            {
                await action(ct).ConfigureAwait(false);
                return true;
            },
            options,
            timeProvider,
            cancellationToken);
    }
}
