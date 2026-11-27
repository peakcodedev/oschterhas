using System.Security.Claims;
using Oschterhas.Api.Auth;

namespace Oschterhas.Api.Http;

/// <summary>
/// Logs rejected API requests (401, 403, 429) as security events. No IP addresses (privacy: searchers'
/// IPs are never stored), but method, path, status and the signed-in user id if there is one.
/// </summary>
internal sealed class SecurityEventLogging(RequestDelegate next, ILogger<SecurityEventLogging> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var status = context.Response.StatusCode;
        if (status is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden or StatusCodes.Status429TooManyRequests
            && context.Request.Path.StartsWithSegments("/api"))
        {
            logger.LogWarning(
                "Security event: {Method} {Path} answered {StatusCode} (user {UserId})",
                context.Request.Method,
                context.Request.Path.Value,
                status,
                context.User.FindFirstValue(AuthenticationSetup.UserIdClaim) ?? "anonymous");
        }
    }
}
