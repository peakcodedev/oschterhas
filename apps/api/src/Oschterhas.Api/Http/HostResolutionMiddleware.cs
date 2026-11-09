using Oschterhas.Core.Hosting;

namespace Oschterhas.Api.Http;

/// <summary>Resolves the request host (subdomain or custom domain) once per request and stores the result.</summary>
internal sealed class HostResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IHostResolver resolver)
    {
        var resolution = await resolver.ResolveAsync(context.Request.Host.Host, context.RequestAborted);
        context.Features.Set(new HostResolutionFeature(resolution));
        await next(context);
    }
}

internal sealed record HostResolutionFeature(HostResolution Resolution);

internal static class HostResolutionHttpContextExtensions
{
    /// <summary>Result of <see cref="HostResolutionMiddleware"/>; <see cref="HostResolution.Unknown"/> if it did not run.</summary>
    public static HostResolution GetHostResolution(this HttpContext context) =>
        context.Features.Get<HostResolutionFeature>()?.Resolution ?? HostResolution.Unknown;
}
