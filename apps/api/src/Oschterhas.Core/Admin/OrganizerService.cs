using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oschterhas.Common.Results;
using Oschterhas.Core.Identity;
using Oschterhas.Core.Users;
using Oschterhas.Data;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Admin;

internal sealed class OrganizerService(
    IInstanceRepository instances,
    IUserRepository users,
    IIdentityAdminClient identity,
    IOptions<IdentityAdminOptions> identityOptions,
    TimeProvider timeProvider,
    ILogger<OrganizerService> logger) : IOrganizerService
{
    private static readonly Error OrganizerNotFound = Error.NotFound("organizer.not_found", "Organizer not found.");

    public async Task<Result<OrganizerDto>> AddAsync(
        CurrentUser actor,
        Guid instanceId,
        AddOrganizerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!actor.IsPlatformAdmin)
        {
            return InstanceAccess.PlatformAdminRequired;
        }

        var input = Validate(request);
        if (input.IsFailure)
        {
            return input.Error!;
        }

        var (email, displayName) = input.Value;
        var instance = await instances.FindForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        if (instance is null)
        {
            return InstanceAccess.NotFound;
        }

        var account = await identity.FindOrCreateUserAsync(email, displayName, cancellationToken).ConfigureAwait(false);
        if (account.IsFailure)
        {
            return account.Error!;
        }

        var issuer = identityOptions.Value.Issuer;
        var now = timeProvider.GetUtcNow();
        var user = await users.FindByIdentityAsync(issuer, account.Value.Subject, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                IdentityIssuer = issuer,
                IdentitySubject = account.Value.Subject,
                Email = email,
                DisplayName = displayName,
                CreatedAt = now,
            };
            users.Add(user);
        }

        if (instance.Organizers.Any(o => o.UserId == user.Id))
        {
            return Error.Conflict("organizer.already_assigned", "This person already organizes this search.");
        }

        instance.Organizers.Add(new InstanceOrganizer { InstanceId = instance.Id, UserId = user.Id, User = user, CreatedAt = now });
        await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Organizer {UserId} assigned to instance {InstanceId} by {ActorId} (new account: {Created})",
            user.Id,
            instance.Id,
            actor.Id,
            account.Value.Created);

        if (account.Value.Created)
        {
            var invited = await identity.SendInvitationAsync(account.Value.Subject, cancellationToken).ConfigureAwait(false);
            if (invited.IsFailure)
            {
                logger.LogWarning("Invitation for organizer {UserId} could not be sent", user.Id);
                // The organizer is assigned; the invitation can be sent again later.
                return Error.Unexpected("organizer.invitation_failed", "The organizer was added, but the invitation could not be sent. Please try again.");
            }
        }

        return user.ToOrganizerDto();
    }

    public async Task<Result> RemoveAsync(CurrentUser actor, Guid instanceId, Guid userId, CancellationToken cancellationToken)
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

        var organizer = instance.Organizers.FirstOrDefault(o => o.UserId == userId);
        if (organizer is null)
        {
            return OrganizerNotFound;
        }

        instance.Organizers.Remove(organizer);
        await instances.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Organizer {UserId} removed from instance {InstanceId} by {ActorId}", userId, instanceId, actor.Id);
        return Result.Success();
    }

    public async Task<Result> ResendInvitationAsync(CurrentUser actor, Guid instanceId, Guid userId, CancellationToken cancellationToken)
    {
        if (!actor.IsPlatformAdmin)
        {
            return InstanceAccess.PlatformAdminRequired;
        }

        var instance = await instances.FindForUpdateAsync(instanceId, cancellationToken).ConfigureAwait(false);
        var organizer = instance?.Organizers.FirstOrDefault(o => o.UserId == userId);
        if (organizer is null)
        {
            return instance is null ? InstanceAccess.NotFound : OrganizerNotFound;
        }

        if (organizer.User.LastLoginAt is not null)
        {
            return Error.Conflict("organizer.already_active", "This person has already signed in.");
        }

        var sent = await identity.SendInvitationAsync(organizer.User.IdentitySubject, cancellationToken).ConfigureAwait(false);
        return sent.IsSuccess
            ? Result.Success()
            : Error.Unexpected("organizer.invitation_failed", "The invitation could not be sent. Please try again.");
    }

    private static Result<(string Email, string DisplayName)> Validate(AddOrganizerRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (email.Length is 0 or > FieldLengths.Email
            || !MailAddress.TryCreate(email, out var parsed)
            || !string.Equals(parsed.Address, email, StringComparison.Ordinal))
        {
            return Error.Validation("organizer.email_invalid", "Please enter a valid e-mail address.");
        }

        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        if (displayName.Length == 0 || displayName.Length > FieldLengths.DisplayName)
        {
            return Error.Validation("organizer.name_invalid", "Please enter a name (at most 200 characters).");
        }

        return (email, displayName);
    }
}
