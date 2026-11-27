namespace Oschterhas.Api.Http;

/// <summary>
/// Security headers for every response (API and, in production, the Angular app served from wwwroot).
/// Scripts only from our own origin: no inline scripts, no eval. Styles allow 'unsafe-inline' because
/// Angular component styles and Leaflet's positioning use inline styles (no script execution possible).
/// </summary>
internal static class SecurityHeaders
{
    /// <summary>Map tiles: swisstopo WMTS and OpenStreetMap.</summary>
    private const string TileHosts = "https://wmts.geo.admin.ch https://tile.openstreetmap.org";

    public static string ContentSecurityPolicy(string? identityOrigin, bool upgradeInsecureRequests)
    {
        // The logout form posts to /api/auth/logout, which redirects to Keycloak: form-action must allow it.
        var formAction = string.IsNullOrEmpty(identityOrigin) ? "'self'" : $"'self' {identityOrigin}";
        var policy = string.Join(
            "; ",
            "default-src 'self'",
            "script-src 'self'",
            "style-src 'self' 'unsafe-inline'",
            $"img-src 'self' data: {TileHosts}",
            // The service worker loads the tiles with fetch(), which counts as connect-src (not img-src);
            // without the tile hosts here, every tile fails as "504 Gateway Timeout (from service worker)".
            $"connect-src 'self' {TileHosts}",
            "font-src 'self'",
            "manifest-src 'self'",
            "worker-src 'self'",
            "object-src 'none'",
            "base-uri 'self'",
            "frame-ancestors 'none'",
            $"form-action {formAction}");
        return upgradeInsecureRequests ? policy + "; upgrade-insecure-requests" : policy;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string? identityOrigin, bool isDevelopment)
    {
        var csp = ContentSecurityPolicy(identityOrigin, upgradeInsecureRequests: !isDevelopment);
        return app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = csp;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                // Origin only: OpenStreetMap's tile policy requires a referrer, full URLs are not needed.
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=(), payment=(), usb=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";
                return Task.CompletedTask;
            });
            return next(context);
        });
    }
}
