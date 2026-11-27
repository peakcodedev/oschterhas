using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oschterhas.Api.Http;
using Oschterhas.Core.Domains;

namespace Oschterhas.Api.Controllers;

/// <summary>
/// "ask" endpoint for Caddy's on-demand TLS: Caddy only requests a certificate if this answers 200.
/// Allowed: our own hosts (base domain, admin, existing instances) and verified custom domains.
/// </summary>
[ApiController]
[AllowAnonymous]
[InternalOnly]
public sealed class InternalTlsController(ICustomDomainService customDomains) : ControllerBase
{
    [HttpGet("/internal/tls/allowed")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Allowed([FromQuery] string? domain, CancellationToken cancellationToken) =>
        !string.IsNullOrWhiteSpace(domain) && await customDomains.IsTlsAllowedAsync(domain, cancellationToken)
            ? Ok()
            : NotFound();
}
