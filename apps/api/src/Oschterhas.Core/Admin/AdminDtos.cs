using Oschterhas.Core.Gpx;
using Oschterhas.Data.Entities;

namespace Oschterhas.Core.Admin;

public sealed record InstanceSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    InstanceStatus Status,
    DateOnly? EventDate,
    int TotalHares,
    int FoundHares,
    string PublicUrl);

public sealed record InstanceDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    DateOnly? EventDate,
    InstanceStatus Status,
    int TotalHares,
    int FoundHares,
    string PublicUrl,
    string SubdomainUrl,
    string? CustomDomain,
    CustomDomainStatus CustomDomainStatus,
    IReadOnlyList<OrganizerDto> Organizers);

public sealed record OrganizerDto(Guid UserId, string Email, string DisplayName, bool InvitationPending);

public sealed record CreateInstanceRequest(string Slug, string Name, string? Description, DateOnly? EventDate);

/// <summary>Full update. Organizers may not change the slug.</summary>
public sealed record UpdateInstanceRequest(string Slug, string Name, string? Description, DateOnly? EventDate);

public sealed record SetInstanceStatusRequest(InstanceStatus Status);

public sealed record SlugCheckDto(string Slug, bool Available, string? Reason);

public sealed record AddOrganizerRequest(string Email, string DisplayName);

public sealed record AdminHareDto(
    Guid Id,
    int Number,
    string? Name,
    string? Description,
    double Lat,
    double Lon,
    double? Elevation,
    string? SourceWaypointId,
    DateTimeOffset? FoundAt,
    string? FoundBy);

public sealed record SaveHareRequest(string? Name, string? Description, double Lat, double Lon);

public enum GpxImportMode
{
    /// <summary>Delete all hares of the instance and import the file.</summary>
    Replace,

    /// <summary>Update hares with the same swisstopo waypoint id, add the others, keep the rest.</summary>
    Merge,
}

public sealed record GpxPreviewDto(
    IReadOnlyList<GpxWaypoint> Waypoints,
    IReadOnlyList<GpxWarning> Warnings,
    int ExistingHares,
    int ExistingFound,
    int MatchingWaypoints);

public sealed record GpxImportResultDto(int Created, int Updated, int Deleted, IReadOnlyList<GpxWarning> Warnings);
