namespace Oschterhas.Core.Events;

/// <summary>A live update for everyone watching an instance; sent as a server-sent event.</summary>
/// <param name="Type">SSE event name, see the constants.</param>
/// <param name="Data">Payload, serialized as JSON.</param>
public sealed record InstanceEvent(string Type, object Data)
{
    public const string HareFound = "hare.found";
    public const string HareReset = "hare.reset";
    public const string HaresChanged = "hares.changed";
    public const string InstanceStatus = "instance.status";
    public const string Ping = "ping";
    public const string Ready = "ready";

    /// <summary>Keeps idle connections open through proxies.</summary>
    public static readonly InstanceEvent Heartbeat = new(Ping, new { });
}

public sealed record HareFoundEventData(Guid HareId, DateTimeOffset FoundAt, string? FoundBy);
