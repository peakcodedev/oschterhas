using Microsoft.AspNetCore.Mvc;
using Oschterhas.Core.Admin;

namespace Oschterhas.Api.Controllers;

[Route("api/instances")]
public sealed class InstancesController(IInstanceAdminService instances) : AdminControllerBase
{
    /// <summary>Platform admins see all instances, organizers their own.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InstanceSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstanceSummaryDto>>> List(CancellationToken cancellationToken) =>
        FromResult(await instances.ListAsync(Actor, cancellationToken));

    [HttpPost]
    [ProducesResponseType<InstanceDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<InstanceDetailDto>> Create(CreateInstanceRequest request, CancellationToken cancellationToken)
    {
        var result = await instances.CreateAsync(Actor, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { instanceId = result.Value.Id }, result.Value)
            : FromResult(result);
    }

    [HttpGet("check-slug")]
    [ProducesResponseType<SlugCheckDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SlugCheckDto>> CheckSlug([FromQuery] string? slug, CancellationToken cancellationToken) =>
        FromResult(await instances.CheckSlugAsync(Actor, slug, cancellationToken));

    [HttpGet("{instanceId:guid}")]
    [ProducesResponseType<InstanceDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InstanceDetailDto>> Get(Guid instanceId, CancellationToken cancellationToken) =>
        FromResult(await instances.GetAsync(Actor, instanceId, cancellationToken));

    [HttpPut("{instanceId:guid}")]
    [ProducesResponseType<InstanceDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<InstanceDetailDto>> Update(Guid instanceId, UpdateInstanceRequest request, CancellationToken cancellationToken) =>
        FromResult(await instances.UpdateAsync(Actor, instanceId, request, cancellationToken));

    /// <summary>Start (Active) or end (Ended) the search, or set it back to Draft.</summary>
    [HttpPost("{instanceId:guid}/status")]
    [ProducesResponseType<InstanceDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InstanceDetailDto>> SetStatus(Guid instanceId, SetInstanceStatusRequest request, CancellationToken cancellationToken) =>
        FromResult(await instances.SetStatusAsync(Actor, instanceId, request, cancellationToken));

    [HttpDelete("{instanceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(Guid instanceId, CancellationToken cancellationToken) =>
        FromResult(await instances.DeleteAsync(Actor, instanceId, cancellationToken));
}
