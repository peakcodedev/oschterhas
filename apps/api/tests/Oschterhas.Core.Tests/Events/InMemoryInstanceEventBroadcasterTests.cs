using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Oschterhas.Core.Events;

namespace Oschterhas.Core.Tests.Events;

public class InMemoryInstanceEventBroadcasterTests
{
    private static readonly Guid Tann = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    private readonly FakeTimeProvider _time = new();
    private readonly InMemoryInstanceEventBroadcaster _broadcaster;

    public InMemoryInstanceEventBroadcasterTests() =>
        _broadcaster = new InMemoryInstanceEventBroadcaster(
            Options.Create(new EventStreamOptions { HeartbeatInterval = TimeSpan.FromSeconds(25), BufferSize = 3 }),
            _time);

    [Fact]
    public async Task Subscriber_ReceivesEventsOfItsInstanceOnly()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using var events = _broadcaster.SubscribeAsync(Tann, cts.Token).GetAsyncEnumerator(cts.Token);
        var next = events.MoveNextAsync();
        await WaitForSubscriber(Tann);

        _broadcaster.Publish(Other, Event("other"));
        _broadcaster.Publish(Tann, Event("tann"));

        Assert.True(await next);
        Assert.Equal("tann", events.Current.Type);
    }

    [Fact]
    public async Task SendsHeartbeat_WhenNothingHappens()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using var events = _broadcaster.SubscribeAsync(Tann, cts.Token).GetAsyncEnumerator(cts.Token);
        var next = events.MoveNextAsync();
        await WaitForSubscriber(Tann);

        _time.Advance(TimeSpan.FromSeconds(25));

        Assert.True(await next);
        Assert.Same(InstanceEvent.Heartbeat, events.Current);
    }

    [Fact]
    public async Task Unsubscribes_WhenConnectionCloses()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var events = _broadcaster.SubscribeAsync(Tann, cts.Token).GetAsyncEnumerator(cts.Token);
        var next = events.MoveNextAsync();
        await WaitForSubscriber(Tann);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await next);
        await events.DisposeAsync();
        Assert.Equal(0, _broadcaster.CountSubscribers(Tann));
    }

    [Fact]
    public async Task SlowSubscriber_KeepsOnlyTheNewestEvents()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using var events = _broadcaster.SubscribeAsync(Tann, cts.Token).GetAsyncEnumerator(cts.Token);
        var first = events.MoveNextAsync();
        await WaitForSubscriber(Tann);
        _broadcaster.Publish(Tann, Event("1"));
        Assert.True(await first);

        foreach (var name in new[] { "2", "3", "4", "5" })
        {
            _broadcaster.Publish(Tann, Event(name));
        }

        var received = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            Assert.True(await events.MoveNextAsync());
            received.Add(events.Current.Type);
        }

        Assert.Equal(["3", "4", "5"], received);
    }

    [Fact]
    public async Task SeveralSubscribers_AllReceiveTheEvent()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using var a = _broadcaster.SubscribeAsync(Tann, cts.Token).GetAsyncEnumerator(cts.Token);
        await using var b = _broadcaster.SubscribeAsync(Tann, cts.Token).GetAsyncEnumerator(cts.Token);
        var nextA = a.MoveNextAsync();
        var nextB = b.MoveNextAsync();
        await WaitForSubscriber(Tann, expected: 2);

        _broadcaster.Publish(Tann, Event("found"));

        Assert.True(await nextA);
        Assert.True(await nextB);
        Assert.Equal("found", a.Current.Type);
        Assert.Equal("found", b.Current.Type);
    }

    [Fact]
    public void Publish_WithoutSubscribers_DoesNothing()
    {
        _broadcaster.Publish(Tann, Event("nobody listens"));

        Assert.Equal(0, _broadcaster.CountSubscribers(Tann));
    }

    private static InstanceEvent Event(string type) => new(type, new { });

    /// <summary>The subscription registers when the iterator starts; wait until that happened.</summary>
    private async Task WaitForSubscriber(Guid instanceId, int expected = 1)
    {
        for (var i = 0; i < 100 && _broadcaster.CountSubscribers(instanceId) < expected; i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Equal(expected, _broadcaster.CountSubscribers(instanceId));
    }
}
