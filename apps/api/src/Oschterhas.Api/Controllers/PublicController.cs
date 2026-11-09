using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oschterhas.Api.Http;
using Oschterhas.Common.Results;
using Oschterhas.Core.Events;
using Oschterhas.Core.Hares;
using Oschterhas.Core.Hosting;
using Oschterhas.Core.Public;

namespace Oschterhas.Api.Controllers;

/// <summary>Endpoints for searchers. The instance always comes from the host name, never from the path.</summary>
[ApiController]
[Route("api/public")]
public sealed class PublicController(
    IPublicInstanceService instances,
    IFoundService found,
    IInstanceEventBroadcaster events) : ControllerBase
{
    private static readonly Error InstanceNotFound = Error.NotFound("instance.not_found", "No search at this address.");

    [HttpGet("instance")]
    [ProducesResponseType<PublicInstanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PublicInstanceDto>> GetInstance(CancellationToken cancellationToken)
    {
        if (CurrentInstanceId() is not { } instanceId)
        {
            return Problems.FromError(InstanceNotFound);
        }

        var result = await instances.GetAsync(instanceId, cancellationToken);
        return result.IsSuccess ? result.Value : Problems.FromError(result.Error!);
    }

    /// <summary>Marks a hare of this instance as found. 409 if it was already found or the search is not running.</summary>
    [HttpPost("hares/{hareId:guid}/found")]
    [EnableRateLimiting(RateLimiting.MarkFound)]
    [ProducesResponseType<HareFoundDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<HareFoundDto>> MarkFound(
        Guid hareId,
        [FromBody] MarkFoundRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentInstanceId() is not { } instanceId)
        {
            return Problems.FromError(InstanceNotFound);
        }

        var result = await found.MarkFoundAsync(instanceId, hareId, request.Nickname, cancellationToken);
        return result.IsSuccess ? result.Value : Problems.FromError(result.Error!);
    }

    /// <summary>Live updates of this instance as server-sent events (ready, hare.found, hare.reset, hares.changed, instance.status, ping).</summary>
    [HttpGet("events")]
    [EnableRateLimiting(RateLimiting.EventStream)]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public IResult Events(CancellationToken cancellationToken)
    {
        if (CurrentInstanceId() is not { } instanceId)
        {
            return Problems.FromError(InstanceNotFound).ToResult();
        }

        // Ask reverse proxies (nginx-style) not to buffer the stream.
        Response.Headers["X-Accel-Buffering"] = "no";
        return TypedResults.ServerSentEvents(ToSse(events.SubscribeAsync(instanceId, cancellationToken), cancellationToken));
    }

    private Guid? CurrentInstanceId() =>
        HttpContext.GetHostResolution() is { Kind: HostKind.Instance, InstanceId: { } id } ? id : null;

    /// <summary>Browser reconnect delay after a dropped connection (sent with the first event).</summary>
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(3);

    private static async IAsyncEnumerable<SseItem<object>> ToSse(
        IAsyncEnumerable<InstanceEvent> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Without a first event the response (and its headers) would only be sent with the first update.
        yield return new SseItem<object>(new { }, InstanceEvent.Ready) { ReconnectionInterval = ReconnectDelay };

        await foreach (var instanceEvent in source.WithCancellation(cancellationToken))
        {
            yield return new SseItem<object>(instanceEvent.Data, instanceEvent.Type);
        }
    }
}
