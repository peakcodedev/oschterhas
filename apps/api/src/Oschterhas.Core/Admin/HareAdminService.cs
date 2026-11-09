using Oschterhas.Common.Results;
using Oschterhas.Core.Events;
using Oschterhas.Core.Users;
using Oschterhas.Data;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Admin;

internal sealed class HareAdminService(
    InstanceAccess access,
    IHareRepository hares,
    IInstanceEventBroadcaster events,
    TimeProvider timeProvider) : IHareAdminService
{
    private static readonly Error HareNotFound = Error.NotFound("hare.not_found", "Hare not found.");
    private static readonly Error Changed = Error.Conflict("hare.changed", "Someone else changed this hare in the meantime. Please reload.");

    public async Task<Result<IReadOnlyList<AdminHareDto>>> ListAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        var allowed = await access.EnsureCanManageAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var list = await hares.ListForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<AdminHareDto>>.Success(list.Select(h => h.ToAdminDto()).ToList());
    }

    public async Task<Result<AdminHareDto>> CreateAsync(CurrentUser actor, Guid instanceId, SaveHareRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var allowed = await access.EnsureCanManageAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var input = HareInput.Validate(request);
        if (input.IsFailure)
        {
            return input.Error!;
        }

        var existing = await hares.ListForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        var now = timeProvider.GetUtcNow();
        var hare = new Hare
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            Number = existing.Count == 0 ? 1 : existing.Max(h => h.Number) + 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        input.Value.ApplyTo(hare);
        hares.Add(hare);
        await hares.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        PublishChanged(instanceId);
        return hare.ToAdminDto();
    }

    public async Task<Result<AdminHareDto>> UpdateAsync(
        CurrentUser actor,
        Guid instanceId,
        Guid hareId,
        SaveHareRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hare = await LoadAsync(actor, instanceId, hareId, cancellationToken).ConfigureAwait(false);
        if (hare.IsFailure)
        {
            return hare.Error!;
        }

        var input = HareInput.Validate(request);
        if (input.IsFailure)
        {
            return input.Error!;
        }

        input.Value.ApplyTo(hare.Value);
        hare.Value.UpdatedAt = timeProvider.GetUtcNow();
        if (!await TrySaveAsync(cancellationToken).ConfigureAwait(false))
        {
            return Changed;
        }

        PublishChanged(instanceId);
        return hare.Value.ToAdminDto();
    }

    public async Task<Result> DeleteAsync(CurrentUser actor, Guid instanceId, Guid hareId, CancellationToken cancellationToken)
    {
        var hare = await LoadAsync(actor, instanceId, hareId, cancellationToken).ConfigureAwait(false);
        if (hare.IsFailure)
        {
            return hare.Error!;
        }

        hares.Remove(hare.Value);
        if (!await TrySaveAsync(cancellationToken).ConfigureAwait(false))
        {
            return Changed;
        }

        PublishChanged(instanceId);
        return Result.Success();
    }

    public async Task<Result<AdminHareDto>> ResetAsync(CurrentUser actor, Guid instanceId, Guid hareId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(actor, instanceId, hareId, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var hare = loaded.Value;
        if (hare.FoundAt is null)
        {
            return hare.ToAdminDto();
        }

        var now = timeProvider.GetUtcNow();
        hare.FoundAt = null;
        hare.FoundBy = null;
        hare.UpdatedAt = now;
        hares.AddFoundEvent(new FoundEvent
        {
            Id = Guid.NewGuid(),
            HareId = hare.Id,
            Type = FoundEventType.Reset,
            UserId = actor.Id,
            CreatedAt = now,
        });
        if (!await TrySaveAsync(cancellationToken).ConfigureAwait(false))
        {
            return Changed;
        }

        events.Publish(instanceId, new InstanceEvent(InstanceEvent.HareReset, new { hareId = hare.Id }));
        return hare.ToAdminDto();
    }

    private async Task<Result<Hare>> LoadAsync(CurrentUser actor, Guid instanceId, Guid hareId, CancellationToken cancellationToken)
    {
        var allowed = await access.EnsureCanManageAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var hare = await hares.FindForUpdateAsync(instanceId, hareId, cancellationToken).ConfigureAwait(false);
        return hare is null ? HareNotFound : hare;
    }

    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await hares.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (ConcurrencyConflictException)
        {
            return false;
        }
    }

    private void PublishChanged(Guid instanceId) =>
        events.Publish(instanceId, new InstanceEvent(InstanceEvent.HaresChanged, new { }));
}

internal sealed record HareInput(string? Name, string? Description, double Lat, double Lon)
{
    public static Result<HareInput> Validate(SaveHareRequest request)
    {
        if (request.Lat is not (>= -90 and <= 90) || request.Lon is not (>= -180 and <= 180) || !double.IsFinite(request.Lat) || !double.IsFinite(request.Lon))
        {
            return Error.Validation("hare.coordinates_invalid", "Latitude/longitude are out of range.");
        }

        var name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        if (name?.Length > FieldLengths.HareName)
        {
            return Error.Validation("hare.name_too_long", $"Name may have at most {FieldLengths.HareName} characters.");
        }

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (description?.Length > FieldLengths.HareDescription)
        {
            return Error.Validation("hare.description_too_long", $"Description may have at most {FieldLengths.HareDescription} characters.");
        }

        return new HareInput(name, description, request.Lat, request.Lon);
    }

    public void ApplyTo(Hare hare)
    {
        hare.Name = Name;
        hare.Description = Description;
        hare.Lat = Lat;
        hare.Lon = Lon;
    }
}
