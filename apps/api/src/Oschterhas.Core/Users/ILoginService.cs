using Oschterhas.Common.Results;

namespace Oschterhas.Core.Users;

public interface ILoginService
{
    /// <summary>Maps a successful login at Peakcode Identity to an Oschterhas user. Fails if there is no active user.</summary>
    Task<Result<CurrentUser>> SignInAsync(ExternalIdentity identity, CancellationToken cancellationToken);

    /// <summary>Reloads the user of an existing session; null if it no longer exists or was disabled.</summary>
    Task<CurrentUser?> GetActiveUserAsync(Guid userId, CancellationToken cancellationToken);
}
