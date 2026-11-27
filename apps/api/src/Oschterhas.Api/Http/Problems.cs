using Microsoft.AspNetCore.Mvc;
using Oschterhas.Common.Results;

namespace Oschterhas.Api.Http;

/// <summary>Turns a Core <see cref="Error"/> into an RFC 9457 problem response with the error code as extension.</summary>
internal static class Problems
{
    public static ObjectResult FromError(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var (status, section) = error.Kind switch
        {
            ErrorKind.Validation => (StatusCodes.Status422UnprocessableEntity, "15.5.21"),
            ErrorKind.NotFound => (StatusCodes.Status404NotFound, "15.5.5"),
            ErrorKind.Conflict => (StatusCodes.Status409Conflict, "15.5.10"),
            ErrorKind.Forbidden => (StatusCodes.Status403Forbidden, "15.5.4"),
            _ => (StatusCodes.Status500InternalServerError, "15.6.1"),
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Message,
            Type = $"https://tools.ietf.org/html/rfc9110#section-{section}",
            Extensions = { ["code"] = error.Code },
        };

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}

internal static class ObjectResultExtensions
{
    /// <summary>For actions that return <see cref="IResult"/> (e.g. the SSE endpoint).</summary>
    public static IResult ToResult(this ObjectResult result) =>
        TypedResults.Json(result.Value, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", result.StatusCode);
}
