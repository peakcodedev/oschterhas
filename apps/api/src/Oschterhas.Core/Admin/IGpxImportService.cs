using Oschterhas.Common.Results;
using Oschterhas.Core.Users;

namespace Oschterhas.Core.Admin;

public interface IGpxImportService
{
    /// <summary>Parses the file and compares it with the current hares; saves nothing.</summary>
    Task<Result<GpxPreviewDto>> PreviewAsync(CurrentUser actor, Guid instanceId, Stream gpx, CancellationToken cancellationToken);

    Task<Result<GpxImportResultDto>> ImportAsync(CurrentUser actor, Guid instanceId, Stream gpx, GpxImportMode mode, CancellationToken cancellationToken);
}
