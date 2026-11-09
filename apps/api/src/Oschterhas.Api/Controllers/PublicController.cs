using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Http;
using Oschterhas.Common.Results;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Public;

namespace Oschterhas.Api.Controllers;

/// <summary>Endpoints for searchers. The instance always comes from the host name, never from the path.</summary>
[ApiController]
[Route("api/public")]
public sealed class PublicController(IPublicInstanceService instances) : ControllerBase
{
    private static readonly Error InstanceNotFound = Error.NotFound("instance.not_found", "No search at this address.");

    [HttpGet("instance")]
    [ProducesResponseType<PublicInstanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PublicInstanceDto>> GetInstance(CancellationToken cancellationToken)
    {
        var host = HttpContext.GetHostResolution();
        if (host is not { Kind: HostKind.Instance, InstanceId: { } instanceId })
        {
            return Problems.FromError(InstanceNotFound);
        }

        var result = await instances.GetAsync(instanceId, cancellationToken);
        return result.IsSuccess ? result.Value : Problems.FromError(result.Error!);
    }
}
