namespace Oschterhas.Core.Events;

public sealed class EventStreamOptions
{
    public const string SectionName = "EventStream";

    /// <summary>Interval of the heartbeat that keeps idle connections open through proxies.</summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(25);

    /// <summary>Buffered events per connection; on overflow the oldest are dropped (the client reloads on reconnect).</summary>
    public int BufferSize { get; set; } = 100;
}
