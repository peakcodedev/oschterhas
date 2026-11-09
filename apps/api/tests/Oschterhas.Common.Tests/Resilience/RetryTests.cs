using Microsoft.Extensions.Time.Testing;
using Oschterhas.Common.Resilience;

namespace Oschterhas.Common.Tests.Resilience;

public class RetryTests
{
    private static readonly RetryOptions NoDelay = new()
    {
        MaxAttempts = 3,
        BaseDelay = TimeSpan.Zero,
        MaxDelay = TimeSpan.Zero,
    };

    [Fact]
    public async Task ReturnsResult_WhenFirstAttemptSucceeds()
    {
        var calls = 0;

        var result = await Retry.ExecuteAsync(
            _ => { calls++; return Task.FromResult("ok"); },
            NoDelay,
            TimeProvider.System,
            TestContext.Current.CancellationToken);

        Assert.Equal("ok", result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RetriesUntilSuccess()
    {
        var calls = 0;

        var result = await Retry.ExecuteAsync(
            _ => ++calls < 3 ? throw new TimeoutException() : Task.FromResult(calls),
            NoDelay,
            TimeProvider.System,
            TestContext.Current.CancellationToken);

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task ThrowsLastException_AfterMaxAttempts()
    {
        var calls = 0;

        await Assert.ThrowsAsync<TimeoutException>(() => Retry.ExecuteAsync<int>(
            _ => { calls++; throw new TimeoutException(); },
            NoDelay,
            TimeProvider.System,
            TestContext.Current.CancellationToken));

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task DoesNotRetry_WhenShouldRetryIsFalse()
    {
        var calls = 0;
        var options = NoDelay with { ShouldRetry = ex => ex is TimeoutException };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Retry.ExecuteAsync<int>(
            _ => { calls++; throw new InvalidOperationException(); },
            options,
            TimeProvider.System,
            TestContext.Current.CancellationToken));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DoesNotRetry_OnCancellation()
    {
        var calls = 0;

        await Assert.ThrowsAsync<OperationCanceledException>(() => Retry.ExecuteAsync<int>(
            _ => { calls++; throw new OperationCanceledException(); },
            NoDelay,
            TimeProvider.System,
            TestContext.Current.CancellationToken));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task WaitsWithBackoff_BetweenAttempts()
    {
        var time = new FakeTimeProvider();
        var options = new RetryOptions { MaxAttempts = 3, BaseDelay = TimeSpan.FromSeconds(1) };
        var calls = 0;

        var task = Retry.ExecuteAsync(
            _ => ++calls < 3 ? throw new TimeoutException() : Task.FromResult(calls),
            options,
            time,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        time.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Equal(1, calls);
        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(2, calls);
        time.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(3, await task);
    }

    [Theory]
    [InlineData(1, 200)]
    [InlineData(2, 400)]
    [InlineData(3, 800)]
    [InlineData(10, 5000)]
    public void GetDelay_DoublesAndIsCapped(int failedAttempt, int expectedMs)
    {
        var options = new RetryOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(200),
            MaxDelay = TimeSpan.FromSeconds(5),
        };

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), options.GetDelay(failedAttempt));
    }

    [Fact]
    public async Task RejectsInvalidMaxAttempts()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Retry.ExecuteAsync(
            _ => Task.FromResult(1),
            new RetryOptions { MaxAttempts = 0 },
            TimeProvider.System,
            TestContext.Current.CancellationToken));
    }
}
