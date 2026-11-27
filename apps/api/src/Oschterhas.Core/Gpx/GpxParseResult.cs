namespace Oschterhas.Core.Gpx;

/// <summary>One <c>&lt;wpt&gt;</c> from a GPX file.</summary>
/// <param name="Name">Null when the waypoint has no name or only a default name like "Wegpunkt".</param>
/// <param name="SourceWaypointId">The swisstopo <c>waypoint_id</c>, if present and unique in the file.</param>
public sealed record GpxWaypoint(
    double Lat,
    double Lon,
    double? Elevation,
    string? Name,
    string? Description,
    string? SourceWaypointId);

/// <summary>A problem that did not stop the import, e.g. a skipped waypoint.</summary>
/// <param name="Position">1-based position of the waypoint in the file.</param>
public sealed record GpxWarning(string Code, int Position, string Message);

public sealed record GpxParseResult(IReadOnlyList<GpxWaypoint> Waypoints, IReadOnlyList<GpxWarning> Warnings);
