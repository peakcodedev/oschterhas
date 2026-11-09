using Microsoft.Extensions.Options;
using Oschterhas.Common.Results;
using Oschterhas.Data.Entities;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Users;

internal sealed class LoginService(
    IUserRepository users,
    IOptions<AuthOptions> options,
    TimeProvider timeProvider) : ILoginService
{
    public async Task<Result<CurrentUser>> SignInAsync(ExternalIdentity identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var now = timeProvider.GetUtcNow();
        var user = await users.FindByIdentityAsync(identity.Issuer, identity.Subject, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            if (!options.Value.InitialPlatformAdmins.Contains(identity.Subject))
            {
                return Error.Forbidden("auth.no_access", "This account has no access to Oschterhas.");
            }

            user = new AppUser
            {
                Id = Guid.NewGuid(),
                IdentityIssuer = identity.Issuer,
                IdentitySubject = identity.Subject,
                Email = identity.Email ?? string.Empty,
                DisplayName = identity.DisplayName ?? identity.Email ?? identity.Subject,
                IsPlatformAdmin = true,
                CreatedAt = now,
            };
            users.Add(user);
        }

        if (!user.IsActive)
        {
            return Error.Forbidden("auth.disabled", "This account has been disabled.");
        }

        // Keep display data in sync with Peakcode Identity.
        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            user.Email = identity.Email;
        }

        if (!string.IsNullOrWhiteSpace(identity.DisplayName))
        {
            user.DisplayName = identity.DisplayName;
        }

        user.LastLoginAt = now;
        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.ToCurrentUser();
    }

    public async Task<CurrentUser?> GetActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user is { IsActive: true } ? user.ToCurrentUser() : null;
    }
}

internal static class UserMapping
{
    public static CurrentUser ToCurrentUser(this AppUser user) =>
        new(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin);

    public static UserDto ToDto(this AppUser user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.IsPlatformAdmin,
        user.IsActive,
        user.LastLoginAt is null,
        user.LastLoginAt,
        user.Instances.Select(i => i.InstanceId).ToList());

    public static MeDto ToMeDto(this CurrentUser user) => new(user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin);
}
