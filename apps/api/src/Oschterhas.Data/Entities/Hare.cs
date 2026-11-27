namespace Oschterhas.Data.Entities;

/// <summary>A hiding place, imported from one GPX waypoint.</summary>
public class Hare
{
    public Guid Id { get; set; }

    public Guid InstanceId { get; set; }

    public Instance Instance { get; set; } = null!;

    /// <summary>Running number within the instance, starting at 1.</summary>
    public int Number { get; set; }

    /// <summary>Null for waypoints without a real name; the app then shows "Hase {Number}" in the user's language.</summary>
    public string? Name { get; set; }

    public string? Description { get; set; }

    public double Lat { get; set; }

    public double Lon { get; set; }

    public double? Elevation { get; set; }

    /// <summary>The swisstopo <c>waypoint_id</c>; used to update hares on a re-import.</summary>
    public string? SourceWaypointId { get; set; }

    public DateTimeOffset? FoundAt { get; set; }

    public string? FoundBy { get; set; }

    /// <summary>Concurrency token, mapped to PostgreSQL's <c>xmin</c> system column.</summary>
    public uint Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<FoundEvent> FoundEvents { get; set; } = [];
}
