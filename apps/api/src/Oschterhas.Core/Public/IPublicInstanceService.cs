using Oschterhas.Common.Results;

namespace Oschterhas.Core.Public;

public interface IPublicInstanceService
{
    Task<Result<PublicInstanceDto>> GetAsync(Guid instanceId, CancellationToken cancellationToken);
}
