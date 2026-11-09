using Oschterhas.Common.Results;
using Oschterhas.Core.Users;

namespace Oschterhas.Core.Admin;

public interface IInstanceAdminService
{
    Task<Result<IReadOnlyList<InstanceSummaryDto>>> ListAsync(CurrentUser actor, CancellationToken cancellationToken);

    Task<Result<InstanceDetailDto>> GetAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken);

    Task<Result<InstanceDetailDto>> CreateAsync(CurrentUser actor, CreateInstanceRequest request, CancellationToken cancellationToken);

    Task<Result<InstanceDetailDto>> UpdateAsync(CurrentUser actor, Guid instanceId, UpdateInstanceRequest request, CancellationToken cancellationToken);

    Task<Result<InstanceDetailDto>> SetStatusAsync(CurrentUser actor, Guid instanceId, SetInstanceStatusRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(CurrentUser actor, Guid instanceId, CancellationToken cancellationToken);

    Task<Result<SlugCheckDto>> CheckSlugAsync(CurrentUser actor, string? slug, CancellationToken cancellationToken);
}
