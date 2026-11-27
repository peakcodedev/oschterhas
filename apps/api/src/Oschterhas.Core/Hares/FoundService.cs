using Microsoft.Extensions.Logging;
using Oschterhas.Common.Results;
using Oschterhas.Core.Events;
using Oschterhas.Data;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Hares;

internal sealed class FoundService(
    IHareRepository hares,
    IInstanceEventBroadcaster events,
    TimeProvider timeProvider,
    ILogger<FoundService> logger) : IFoundService
{
    public async Task<Result<HareFoundDto>> MarkFoundAsync(
        Guid instanceId,
        Guid hareId,
        string? nickname,
        CancellationToken cancellationToken)
    {
        var cleanNickname = Nickname.Clean(nickname);
        if (cleanNickname.IsFailure)
        {
            return cleanNickname.Error!;
        }

        var hare = await hares.FindForUpdateAsync(instanceId, hareId, cancellationToken).ConfigureAwait(false);
        if (hare is null)
        {
            return Error.NotFound("hare.not_found", "Hare not found.");
        }

        if (hare.Instance.Status is not InstanceStatus.Active)
        {
            return Error.Conflict("instance.not_active", "The search is not running.");
        }

        if (hare.FoundAt is not null)
        {
            return Error.Conflict("hare.already_found", "This hare has already been found.");
        }

        var now = timeProvider.GetUtcNow();
        hare.FoundAt = now;
        hare.FoundBy = cleanNickname.Value;
        hare.UpdatedAt = now;
        hares.AddFoundEvent(new FoundEvent
        {
            Id = Guid.NewGuid(),
            HareId = hare.Id,
            Type = FoundEventType.Found,
            Nickname = cleanNickname.Value,
            CreatedAt = now,
        });

        try
        {
            await hares.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            // Someone else was faster.
            return Error.Conflict("hare.already_found", "This hare has already been found.");
        }

        // No nickname in the log: it is optional personal data and only needed in the database.
        logger.LogInformation("Hare {HareId} of instance {InstanceId} marked as found", hare.Id, instanceId);
        events.Publish(instanceId, new InstanceEvent(
            InstanceEvent.HareFound,
            new HareFoundEventData(hare.Id, now, cleanNickname.Value)));

        return new HareFoundDto(hare.Id, now, cleanNickname.Value);
    }
}

/// <summary>Rules for the optional nickname a searcher enters.</summary>
internal static class Nickname
{
    public static Result<string?> Clean(string? nickname)
    {
        var trimmed = nickname?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Result<string?>.Success(null);
        }

        if (trimmed.Length > FieldLengths.Nickname)
        {
            return Error.Validation("hare.nickname_too_long", $"The name may have at most {FieldLengths.Nickname} characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            return Error.Validation("hare.nickname_invalid", "The name contains invalid characters.");
        }

        return trimmed;
    }
}
