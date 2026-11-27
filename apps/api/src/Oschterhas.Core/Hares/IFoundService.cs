using Oschterhas.Common.Results;

namespace Oschterhas.Core.Hares;

public interface IFoundService
{
    /// <summary>A searcher marks a hare of the instance as found.</summary>
    Task<Result<HareFoundDto>> MarkFoundAsync(Guid instanceId, Guid hareId, string? nickname, CancellationToken cancellationToken);
}
