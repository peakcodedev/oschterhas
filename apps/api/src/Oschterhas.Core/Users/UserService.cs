using Oschterhas.Common.Results;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Users;

internal sealed class UserService(IUserRepository users) : IUserService
{
    internal static readonly Error NotPlatformAdmin = Error.Forbidden("auth.platform_admin_required", "Only platform admins may do this.");

    public async Task<Result<IReadOnlyList<UserDto>>> ListAsync(CurrentUser actor, CancellationToken cancellationToken)
    {
        if (!actor.IsPlatformAdmin)
        {
            return NotPlatformAdmin;
        }

        var all = await users.ListAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<UserDto>>.Success(all.Select(u => u.ToDto()).ToList());
    }

    public async Task<Result<UserDto>> UpdateAsync(
        CurrentUser actor,
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!actor.IsPlatformAdmin)
        {
            return NotPlatformAdmin;
        }

        // Prevents locking oneself out (and the platform with it).
        if (userId == actor.Id && (request.IsActive == false || request.IsPlatformAdmin == false))
        {
            return Error.Validation("user.cannot_demote_self", "You cannot disable or demote yourself.");
        }

        var user = await users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Error.NotFound("user.not_found", "User not found.");
        }

        user.IsActive = request.IsActive ?? user.IsActive;
        user.IsPlatformAdmin = request.IsPlatformAdmin ?? user.IsPlatformAdmin;
        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.ToDto();
    }
}
