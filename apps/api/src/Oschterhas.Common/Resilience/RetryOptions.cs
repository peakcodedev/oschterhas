namespace Oschterhas.Common.Resilience;

/// <summary>Settings for <see cref="Retry"/>: exponential backoff without jitter.</summary>
public sealed record RetryOptions
{
    /// <summary>Total number of attempts, including the first one. Must be at least 1.</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Delay after the first failed attempt; doubles with every further attempt.</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Upper limit for a single delay.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Decides whether an exception is worth another attempt. Default: every exception.</summary>
    public Func<Exception, bool> ShouldRetry { get; init; } = static _ => true;

    /// <summary>Delay before the next attempt after <paramref name="failedAttempt"/> (1-based) failed.</summary>
    public TimeSpan GetDelay(int failedAttempt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(failedAttempt, 1);

        var factor = Math.Pow(2, failedAttempt - 1);
        var ticks = BaseDelay.Ticks * factor;
        return ticks >= MaxDelay.Ticks ? MaxDelay : TimeSpan.FromTicks((long)ticks);
    }
}
