using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Oschterhas.Core.Events;

/// <summary>
/// Keeps one bounded channel per open connection. Works for a single API process;
/// with several processes PostgreSQL LISTEN/NOTIFY would be needed (see docs/architektur.md).
/// </summary>
internal sealed class InMemoryInstanceEventBroadcaster(IOptions<EventStreamOptions> options, TimeProvider timeProvider)
    : IInstanceEventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, ImmutableList<Channel<InstanceEvent>>> _subscribers = new();
    private readonly EventStreamOptions _options = options.Value;

    public void Publish(Guid instanceId, InstanceEvent instanceEvent)
    {
        ArgumentNullException.ThrowIfNull(instanceEvent);
        if (_subscribers.TryGetValue(instanceId, out var channels))
        {
            foreach (var channel in channels)
            {
                channel.Writer.TryWrite(instanceEvent);
            }
        }
    }

    public int CountSubscribers(Guid instanceId) =>
        _subscribers.TryGetValue(instanceId, out var channels) ? channels.Count : 0;

    public async IAsyncEnumerable<InstanceEvent> SubscribeAsync(
        Guid instanceId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<InstanceEvent>(new BoundedChannelOptions(_options.BufferSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });
        _subscribers.AddOrUpdate(instanceId, _ => [channel], (_, list) => list.Add(channel));

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InstanceEvent next;
                using (var heartbeat = new CancellationTokenSource(_options.HeartbeatInterval, timeProvider))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, heartbeat.Token))
                {
                    try
                    {
                        next = await channel.Reader.ReadAsync(linked.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        next = InstanceEvent.Heartbeat;
                    }
                }

                yield return next;
            }
        }
        finally
        {
            Remove(instanceId, channel);
        }
    }

    private void Remove(Guid instanceId, Channel<InstanceEvent> channel)
    {
        while (_subscribers.TryGetValue(instanceId, out var channels))
        {
            var remaining = channels.Remove(channel);
            var updated = remaining.IsEmpty
                ? _subscribers.TryRemove(new KeyValuePair<Guid, ImmutableList<Channel<InstanceEvent>>>(instanceId, channels))
                : _subscribers.TryUpdate(instanceId, remaining, channels);
            if (updated)
            {
                return;
            }
        }
    }
}
