using Oschterhas.Common.Results;
using Oschterhas.Core.Users;

namespace Oschterhas.Core.Admin;

/// <summary>Organizers edit the hares of their instances.</summary>
public interface IHareAdminService
{
    Task<Result<IReadOnlyList<AdminHareDto>>> ListAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken);

    Task<Result<AdminHareDto>> CreateAsync(CurrentUser actor, Guid instanceId, SaveHareRequest request, CancellationToken cancellationToken);

    Task<Result<AdminHareDto>> UpdateAsync(CurrentUser actor, Guid instanceId, Guid hareId, SaveHareRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(CurrentUser actor, Guid instanceId, Guid hareId, CancellationToken cancellationToken);

    /// <summary>Undo a "found" marking (e.g. a mistake or misuse).</summary>
    Task<Result<AdminHareDto>> ResetAsync(CurrentUser actor, Guid instanceId, Guid hareId, CancellationToken cancellationToken);
}
