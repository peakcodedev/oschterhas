using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oschterhas.Common.Results;
using Oschterhas.Core.Events;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Instances;
using Oschterhas.Core.Users;
using Oschterhas.Data;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Admin;

internal sealed class InstanceAdminService(
    IInstanceRepository instances,
    IHostCacheInvalidator hostCache,
    IInstanceEventBroadcaster events,
    IOptions<HostingOptions> hosting,
    TimeProvider timeProvider,
    ILogger<InstanceAdminService> logger) : IInstanceAdminService
{
    private static readonly Error SlugTaken = Error.Conflict("instance.slug_taken", "This address is already taken.");

    private HostingOptions Hosting => hosting.Value;

    public async Task<Result<IReadOnlyList<InstanceSummaryDto>>> ListAsync(CurrentUser actor, CancellationToken cancellationToken)
    {
        var list = await instances.ListAsync(actor.IsPlatformAdmin ? null : actor.Id, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<InstanceSummaryDto>>.Success(list.Select(i => i.ToSummaryDto(Hosting)).ToList());
    }

    public async Task<Result<InstanceDetailDto>> GetAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await LoadAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        return instance.IsSuccess ? instance.Value.ToDetailDto(Hosting) : instance.Error!;
    }

    public async Task<Result<InstanceDetailDto>> CreateAsync(CurrentUser actor, CreateInstanceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!actor.IsPlatformAdmin)
        {
            return InstanceAccess.PlatformAdminRequired;
        }

        var slug = SlugRules.Normalize(request.Slug);
        if (slug.IsFailure)
        {
            return slug.Error!;
        }

        var texts = InstanceTexts.Validate(request.Name, request.Description);
        if (texts.IsFailure)
        {
            return texts.Error!;
        }

        if (await instances.SlugExistsAsync(slug.Value, cancellationToken).ConfigureAwait(false))
        {
            return SlugTaken;
        }

        var now = timeProvider.GetUtcNow();
        var instance = new Instance
        {
            Id = Guid.NewGuid(),
            Slug = slug.Value,
            Name = texts.Value.Name,
            Description = texts.Value.Description,
            EventDate = request.EventDate,
            Status = InstanceStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
        instances.Add(instance);

        var saved = await SaveAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        hostCache.Invalidate();
        logger.LogInformation("Instance {InstanceId} ({Slug}) created by {ActorId}", instance.Id, instance.Slug, actor.Id);
        return instance.ToDetailDto(Hosting);
    }

    public async Task<Result<InstanceDetailDto>> UpdateAsync(
        CurrentUser actor,
        Guid instanceId,
        UpdateInstanceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var loaded = await LoadAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var instance = loaded.Value;
        var slug = SlugRules.Normalize(request.Slug);
        if (slug.IsFailure)
        {
            return slug.Error!;
        }

        var texts = InstanceTexts.Validate(request.Name, request.Description);
        if (texts.IsFailure)
        {
            return texts.Error!;
        }

        var slugChanged = slug.Value != instance.Slug;
        if (slugChanged)
        {
            if (!actor.IsPlatformAdmin)
            {
                return Error.Forbidden("instance.slug_change_forbidden", "Only platform admins may change the address.");
            }

            if (await instances.SlugExistsAsync(slug.Value, cancellationToken).ConfigureAwait(false))
            {
                return SlugTaken;
            }
        }

        instance.Slug = slug.Value;
        instance.Name = texts.Value.Name;
        instance.Description = texts.Value.Description;
        instance.EventDate = request.EventDate;
        instance.UpdatedAt = timeProvider.GetUtcNow();

        var saved = await SaveAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        if (slugChanged)
        {
            hostCache.Invalidate();
        }

        logger.LogInformation("Instance {InstanceId} updated by {ActorId} (slug {Slug})", instance.Id, actor.Id, instance.Slug);

        // Searchers reload and see the new name and description.
        events.Publish(instance.Id, new InstanceEvent(InstanceEvent.InstanceStatus, new { status = instance.Status }));
        return instance.ToDetailDto(Hosting);
    }

    public async Task<Result<InstanceDetailDto>> SetStatusAsync(
        CurrentUser actor,
        Guid instanceId,
        SetInstanceStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Status))
        {
            return Error.Validation("instance.status_invalid", "Unknown status.");
        }

        var loaded = await LoadAsync(actor, instanceId, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return loaded.Error!;
        }

        var instance = loaded.Value;
        if (instance.Status == request.Status)
        {
            return instance.ToDetailDto(Hosting);
        }

        instance.Status = request.Status;
        instance.UpdatedAt = timeProvider.GetUtcNow();
        var saved = await SaveAsync(cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        events.Publish(instance.Id, new InstanceEvent(InstanceEvent.InstanceStatus, new { status = instance.Status }));
        logger.LogInformation("Instance {InstanceId} status set to {Status} by {ActorId}", instance.Id, instance.Status, actor.Id);
        return instance.ToDetailDto(Hosting);
    }

    public async Task<Result> DeleteAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        if (!actor.IsPlatformAdmin)
        {
            return InstanceAccess.PlatformAdminRequired;
        }

        var instance = await instances.FindForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        if (instance is null)
        {
            return InstanceAccess.NotFound;
        }

        instances.Remove(instance);
        await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        hostCache.Invalidate();
        events.Publish(instance.Id, new InstanceEvent(InstanceEvent.InstanceStatus, new { status = "Deleted" }));
        logger.LogWarning("Instance {InstanceId} ({Slug}) deleted by {ActorId}", instance.Id, instance.Slug, actor.Id);
        return Result.Success();
    }

    public async Task<Result<SlugCheckDto>> CheckSlugAsync(CurrentUser actor, string? slug, CancellationToken cancellationToken)
    {
        if (!actor.IsPlatformAdmin)
        {
            return InstanceAccess.PlatformAdminRequired;
        }

        var normalized = SlugRules.Normalize(slug);
        if (normalized.IsFailure)
        {
            return new SlugCheckDto(slug?.Trim().ToLowerInvariant() ?? string.Empty, false, normalized.Error!.Code);
        }

        var taken = await instances.SlugExistsAsync(normalized.Value, cancellationToken).ConfigureAwait(false);
        return new SlugCheckDto(normalized.Value, !taken, taken ? SlugTaken.Code : null);
    }

    private async Task<Result<Instance>> LoadAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await instances.FindForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        return instance is not null && InstanceAccess.CanManage(actor, instance) ? instance : InstanceAccess.NotFound;
    }

    private async Task<Result> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (UniqueConstraintException)
        {
            return SlugTaken;
        }
        catch (ConcurrencyConflictException)
        {
            return Error.Conflict("instance.changed", "Someone else changed this search in the meantime. Please reload.");
        }
    }
}

internal static class InstanceTexts
{
    public static Result<(string Name, string? Description)> Validate(string? name, string? description)
    {
        var cleanName = name?.Trim() ?? string.Empty;
        if (cleanName.Length == 0)
        {
            return Error.Validation("instance.name_required", "Name is required.");
        }

        if (cleanName.Length > FieldLengths.InstanceName)
        {
            return Error.Validation("instance.name_too_long", $"Name may have at most {FieldLengths.InstanceName} characters.");
        }

        var cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (cleanDescription?.Length > FieldLengths.InstanceDescription)
        {
            return Error.Validation("instance.description_too_long", $"Description may have at most {FieldLengths.InstanceDescription} characters.");
        }

        return (cleanName, cleanDescription);
    }
}
