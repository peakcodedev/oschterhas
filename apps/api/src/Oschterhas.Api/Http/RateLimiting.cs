using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Oschterhas.Api.Http;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    /// <summary>Per client IP. Generous: at an Easter hunt many phones share one mobile-network IP (CGNAT).</summary>
    public int MarkFoundPerMinute { get; set; } = 30;

    /// <summary>Open live-update connections per client IP.</summary>
    public int EventStreamsPerClient { get; set; } = 50;
}

internal static class RateLimiting
{
    public const string MarkFound = "mark-found";
    public const string EventStream = "event-stream";
    public const string GpxUpload = "gpx-upload";
    public const string DomainVerify = "domain-verify";

    public static IServiceCollection AddOschterhasRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();

        return services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteProblemAsync;

            limiter.AddPolicy(MarkFound, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.MarkFoundPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            limiter.AddPolicy(GpxUpload, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            limiter.AddPolicy(DomainVerify, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            limiter.AddPolicy(EventStream, context => RateLimitPartition.GetConcurrencyLimiter(
                ClientKey(context),
                _ => new ConcurrencyLimiterOptions { PermitLimit = options.EventStreamsPerClient, QueueLimit = 0 }));
        });
    }

    /// <summary>
    /// Client IP. Behind the reverse proxy (production) ForwardedHeaders must be configured with the known proxy,
    /// otherwise all clients share the proxy's IP (see roadmap M7).
    /// </summary>
    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async ValueTask WriteProblemAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests. Please wait a moment.",
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Extensions = { ["code"] = "rate_limited" },
        };
        await response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", cancellationToken);
    }
}
