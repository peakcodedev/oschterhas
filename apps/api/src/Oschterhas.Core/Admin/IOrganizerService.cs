using Oschterhas.Common.Results;
using Oschterhas.Core.Users;

namespace Oschterhas.Core.Admin;

/// <summary>Platform admins assign organizers to instances and invite them via Peakcode Identity.</summary>
public interface IOrganizerService
{
    Task<Result<OrganizerDto>> AddAsync(CurrentUser actor, Guid instanceId, AddOrganizerRequest request, CancellationToken cancellationToken);

    Task<Result> RemoveAsync(CurrentUser actor, Guid instanceId, Guid userId, CancellationToken cancellationToken);

    Task<Result> ResendInvitationAsync(CurrentUser actor, Guid instanceId, Guid userId, CancellationToken cancellationToken);
}
