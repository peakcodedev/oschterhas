using Microsoft.AspNetCore.Mvc;
using Oschterhas.Core.Admin;

namespace Oschterhas.Api.Controllers;

[Route("api/instances/{instanceId:guid}/organizers")]
public sealed class OrganizersController(IOrganizerService organizers) : AdminControllerBase
{
    /// <summary>Creates the account at Peakcode Identity if needed, sends the invitation and assigns the person.</summary>
    [HttpPost]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<OrganizerDto>> Add(Guid instanceId, AddOrganizerRequest request, CancellationToken cancellationToken) =>
        FromResult(await organizers.AddAsync(Actor, instanceId, request, cancellationToken));

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Remove(Guid instanceId, Guid userId, CancellationToken cancellationToken) =>
        FromResult(await organizers.RemoveAsync(Actor, instanceId, userId, cancellationToken));

    [HttpPost("{userId:guid}/resend-invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ResendInvitation(Guid instanceId, Guid userId, CancellationToken cancellationToken) =>
        FromResult(await organizers.ResendInvitationAsync(Actor, instanceId, userId, cancellationToken));
}
