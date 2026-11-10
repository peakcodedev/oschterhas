using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oschterhas.Api.Http;
using Oschterhas.Core.Admin;
using Oschterhas.Core.Domains;

namespace Oschterhas.Api.Controllers;

/// <summary>Own domain of an instance, e.g. ostern.ceviduernten.ch (platform admins only).</summary>
[Route("api/instances/{instanceId:guid}/custom-domain")]
public sealed class CustomDomainController(ICustomDomainService customDomains) : AdminControllerBase
{
    /// <summary>Sets (or replaces) the domain; status becomes Pending until the DNS entry points to us.</summary>
    [HttpPut]
    [ProducesResponseType<InstanceDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<InstanceDetailDto>> Set(Guid instanceId, SetCustomDomainRequest request, CancellationToken cancellationToken) =>
        FromResult(await customDomains.SetAsync(Actor, instanceId, request, cancellationToken));

    /// <summary>Checks now whether the domain points to Oschterhas.</summary>
    [HttpPost("verify")]
    [EnableRateLimiting(RateLimiting.DomainVerify)]
    [ProducesResponseType<CustomDomainCheckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<CustomDomainCheckDto>> Verify(Guid instanceId, CancellationToken cancellationToken) =>
        FromResult(await customDomains.VerifyAsync(Actor, instanceId, cancellationToken));

    [HttpDelete]
    [ProducesResponseType<InstanceDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InstanceDetailDto>> Remove(Guid instanceId, CancellationToken cancellationToken) =>
        FromResult(await customDomains.RemoveAsync(Actor, instanceId, cancellationToken));
}
