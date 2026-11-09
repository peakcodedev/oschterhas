using Microsoft.AspNetCore.Mvc;
using Oschterhas.Core.Admin;

namespace Oschterhas.Api.Controllers;

[Route("api/instances/{instanceId:guid}/hares")]
public sealed class HaresController(IHareAdminService hares) : AdminControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminHareDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminHareDto>>> List(Guid instanceId, CancellationToken cancellationToken) =>
        FromResult(await hares.ListAsync(Actor, instanceId, cancellationToken));

    [HttpPost]
    [ProducesResponseType<AdminHareDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<AdminHareDto>> Create(Guid instanceId, SaveHareRequest request, CancellationToken cancellationToken) =>
        FromResult(await hares.CreateAsync(Actor, instanceId, request, cancellationToken));

    [HttpPut("{hareId:guid}")]
    [ProducesResponseType<AdminHareDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<AdminHareDto>> Update(Guid instanceId, Guid hareId, SaveHareRequest request, CancellationToken cancellationToken) =>
        FromResult(await hares.UpdateAsync(Actor, instanceId, hareId, request, cancellationToken));

    [HttpDelete("{hareId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(Guid instanceId, Guid hareId, CancellationToken cancellationToken) =>
        FromResult(await hares.DeleteAsync(Actor, instanceId, hareId, cancellationToken));

    /// <summary>Sets a found hare back to hidden.</summary>
    [HttpPost("{hareId:guid}/reset")]
    [ProducesResponseType<AdminHareDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminHareDto>> Reset(Guid instanceId, Guid hareId, CancellationToken cancellationToken) =>
        FromResult(await hares.ResetAsync(Actor, instanceId, hareId, cancellationToken));
}
