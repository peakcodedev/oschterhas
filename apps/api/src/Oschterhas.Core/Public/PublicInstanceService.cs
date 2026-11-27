using Oschterhas.Common.Results;
using Oschterhas.Data.Repositories;

namespace Oschterhas.Core.Public;

internal sealed class PublicInstanceService(IInstanceRepository instances) : IPublicInstanceService
{
    public async Task<Result<PublicInstanceDto>> GetAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await instances.GetWithHaresAsync(instanceId, cancellationToken).ConfigureAwait(false);
        return instance is null
            ? Error.NotFound("instance.not_found", "Instance not found.")
            : instance.ToPublicDto();
    }
}
