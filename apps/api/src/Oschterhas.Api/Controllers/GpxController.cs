using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oschterhas.Api.Http;
using Oschterhas.Common.Results;
using Oschterhas.Core.Admin;

namespace Oschterhas.Api.Controllers;

[Route("api/instances/{instanceId:guid}/gpx")]
[EnableRateLimiting(RateLimiting.GpxUpload)]
[RequestSizeLimit(MaxUploadBytes + 64 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
public sealed class GpxController(IGpxImportService import) : AdminControllerBase
{
    public const int MaxUploadBytes = 5 * 1024 * 1024;

    /// <summary>Parses the file and shows what would happen; saves nothing.</summary>
    [HttpPost("preview")]
    [ProducesResponseType<GpxPreviewDto>(StatusCodes.Status200OK)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<GpxPreviewDto>> Preview(Guid instanceId, IFormFile file, CancellationToken cancellationToken)
    {
        if (CheckFile(file) is { } problem)
        {
            return problem;
        }

        await using var stream = file.OpenReadStream();
        return FromResult(await import.PreviewAsync(Actor, instanceId, stream, cancellationToken));
    }

    /// <summary>Imports the waypoints: <c>replace</c> deletes all hares first, <c>merge</c> updates by swisstopo waypoint id.</summary>
    [HttpPost]
    [ProducesResponseType<GpxImportResultDto>(StatusCodes.Status200OK)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<GpxImportResultDto>> Import(
        Guid instanceId,
        IFormFile file,
        [FromQuery] GpxImportMode mode,
        CancellationToken cancellationToken)
    {
        if (CheckFile(file) is { } problem)
        {
            return problem;
        }

        await using var stream = file.OpenReadStream();
        return FromResult(await import.ImportAsync(Actor, instanceId, stream, mode, cancellationToken));
    }

    private static ObjectResult? CheckFile(IFormFile? file) => file switch
    {
        null or { Length: 0 } => Problems.FromError(Error.Validation("gpx.file_missing", "Please choose a GPX file.")),
        { Length: > MaxUploadBytes } => Problems.FromError(Error.Validation("gpx.file_too_large", "The file is larger than 5 MB.")),
        _ => null,
    };
}
