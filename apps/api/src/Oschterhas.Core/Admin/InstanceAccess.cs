using Oschterhas.Common.Results;
using Oschterhas.Core.Users;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Admin;

/// <summary>Who may manage an instance: platform admins and the instance's organizers.</summary>
internal sealed class InstanceAccess(IInstanceRepository instances)
{
    /// <summary>Same answer for "does not exist" and "not yours", so organizers cannot probe other instances.</summary>
    public static readonly Error NotFound = Error.NotFound("instance.not_found", "Instance not found.");

    public static readonly Error PlatformAdminRequired = Error.Forbidden("auth.platform_admin_required", "Only platform admins may do this.");

    public async Task<Result> EnsureCanManageAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken)
    {
        var allowed = actor.IsPlatformAdmin
            ? await instances.ExistsAsync(instanceId, cancellationToken).ConfigureAwait(false)
            : await instances.IsOrganizerAsync(instanceId, actor.Id, cancellationToken).ConfigureAwait(false);
        return allowed ? Result.Success() : NotFound;
    }

    public static bool CanManage(CurrentUser actor, Data.Entities.Instance instance) =>
        actor.IsPlatformAdmin || instance.Organizers.Any(o => o.UserId == actor.Id);
}
