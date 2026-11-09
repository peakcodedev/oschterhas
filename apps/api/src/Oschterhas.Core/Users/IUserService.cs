using Oschterhas.Common.Results;

namespace Oschterhas.Core.Users;

/// <summary>Account administration for platform admins.</summary>
public interface IUserService
{
    Task<Result<IReadOnlyList<UserDto>>> ListAsync(CurrentUser actor, CancellationToken cancellationToken);

    Task<Result<UserDto>> UpdateAsync(CurrentUser actor, Guid userId, UpdateUserRequest request, CancellationToken cancellationToken);
}
