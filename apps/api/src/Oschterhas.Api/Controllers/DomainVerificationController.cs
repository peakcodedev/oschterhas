using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Core.Domains;

namespace Oschterhas.Api.Controllers;

/// <summary>
/// Answers the verification request of a custom domain with its token. If our check of
/// http://{domain}/.well-known/oschterhas-verification gets this answer, the domain points to us.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class DomainVerificationController(ICustomDomainService customDomains) : ControllerBase
{
    [HttpGet("/.well-known/oschterhas-verification")]
    [Produces("text/plain")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var token = await customDomains.GetVerificationTokenAsync(Request.Host.Host, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return token is null ? NotFound() : Content(token, "text/plain");
    }
}
