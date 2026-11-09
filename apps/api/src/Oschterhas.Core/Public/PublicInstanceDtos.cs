using Oschterhas.Data.Entities;

namespace Oschterhas.Core.Public;

/// <summary>What searchers see of an instance. <see cref="Hares"/> is null while the search has not started.</summary>
public sealed record PublicInstanceDto(
    string Name,
    string? Description,
    DateOnly? EventDate,
    InstanceStatus Status,
    int TotalHares,
    int FoundHares,
    IReadOnlyList<PublicHareDto>? Hares);

/// <param name="Name">Null when the hare has no own name; show "Hase {Number}".</param>
public sealed record PublicHareDto(
    Guid Id,
    int Number,
    string? Name,
    string? Description,
    double Lat,
    double Lon,
    DateTimeOffset? FoundAt,
    string? FoundBy);
