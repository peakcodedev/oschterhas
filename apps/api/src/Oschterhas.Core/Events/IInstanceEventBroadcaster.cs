namespace Oschterhas.Core.Events;

/// <summary>Distributes live updates to everyone watching an instance (in memory, one API process).</summary>
public interface IInstanceEventBroadcaster
{
    void Publish(Guid instanceId, InstanceEvent instanceEvent);

    /// <summary>Events of the instance until <paramref name="cancellationToken"/> fires; a heartbeat in between.</summary>
    IAsyncEnumerable<InstanceEvent> SubscribeAsync(Guid instanceId, CancellationToken cancellationToken);

    int CountSubscribers(Guid instanceId);
}
