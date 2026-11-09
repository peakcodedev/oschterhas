using Microsoft.Extensions.Options;
using Oschterhas.Common.Results;
using Oschterhas.Core.Events;
using Oschterhas.Core.Gpx;
using Oschterhas.Core.Users;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Admin;

internal sealed class GpxImportService(
    InstanceAccess access,
    IHareRepository hares,
    IInstanceEventBroadcaster events,
    IOptions<GpxParserOptions> parserOptions,
    TimeProvider timeProvider) : IGpxImportService
{
    public async Task<Result<GpxPreviewDto>> PreviewAsync(CurrentUser actor, Guid instanceId, Stream gpx, CancellationToken cancellationToken)
    {
        var allowed = await access.EnsureCanManageAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var parsed = GpxParser.Parse(gpx, parserOptions.Value);
        if (parsed.IsFailure)
        {
            return parsed.Error!;
        }

        var existing = await hares.ListForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        var existingIds = existing.Where(h => h.SourceWaypointId is not null).Select(h => h.SourceWaypointId!).ToHashSet(StringComparer.Ordinal);
        return new GpxPreviewDto(
            parsed.Value.Waypoints,
            parsed.Value.Warnings,
            existing.Count,
            existing.Count(h => h.FoundAt is not null),
            parsed.Value.Waypoints.Count(w => w.SourceWaypointId is { } id && existingIds.Contains(id)));
    }

    public async Task<Result<GpxImportResultDto>> ImportAsync(
        CurrentUser actor,
        Guid instanceId,
        Stream gpx,
        GpxImportMode mode,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(mode))
        {
            return Error.Validation("gpx.mode_invalid", "Unknown import mode.");
        }

        var allowed = await access.EnsureCanManageAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var parsed = GpxParser.Parse(gpx, parserOptions.Value);
        if (parsed.IsFailure)
        {
            return parsed.Error!;
        }

        var now = timeProvider.GetUtcNow();
        var existing = await hares.ListForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        int created = 0, updated = 0, deleted = 0;

        if (mode == GpxImportMode.Replace)
        {
            foreach (var hare in existing)
            {
                hares.Remove(hare);
            }

            deleted = existing.Count;
            var number = 1;
            foreach (var waypoint in parsed.Value.Waypoints)
            {
                hares.Add(NewHare(instanceId, number++, waypoint, now));
                created++;
            }
        }
        else
        {
            var byWaypointId = existing
                .Where(h => h.SourceWaypointId is not null)
                .ToDictionary(h => h.SourceWaypointId!, StringComparer.Ordinal);
            var nextNumber = existing.Count == 0 ? 1 : existing.Max(h => h.Number) + 1;
            foreach (var waypoint in parsed.Value.Waypoints)
            {
                if (waypoint.SourceWaypointId is { } id && byWaypointId.TryGetValue(id, out var hare))
                {
                    hare.Lat = waypoint.Lat;
                    hare.Lon = waypoint.Lon;
                    hare.Elevation = waypoint.Elevation;
                    hare.Name = waypoint.Name;
                    hare.Description = waypoint.Description;
                    hare.UpdatedAt = now;
                    updated++;
                }
                else
                {
                    hares.Add(NewHare(instanceId, nextNumber++, waypoint, now));
                    created++;
                }
            }
        }

        try
        {
            await hares.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Error.Conflict("hare.changed", "Hares were changed in the meantime (e.g. found). Please try again.");
        }

        events.Publish(instanceId, new InstanceEvent(InstanceEvent.HaresChanged, new { }));
        return new GpxImportResultDto(created, updated, deleted, parsed.Value.Warnings);
    }

    private static Hare NewHare(Guid instanceId, int number, GpxWaypoint waypoint, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        InstanceId = instanceId,
        Number = number,
        Name = waypoint.Name,
        Description = waypoint.Description,
        Lat = waypoint.Lat,
        Lon = waypoint.Lon,
        Elevation = waypoint.Elevation,
        SourceWaypointId = waypoint.SourceWaypointId,
        CreatedAt = now,
        UpdatedAt = now,
    };
}
