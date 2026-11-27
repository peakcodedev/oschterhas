using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Oschterhas.Api.Hosting;

/// <summary>
/// In production the API also serves the Angular build from <c>wwwroot</c> (one container, same origin for
/// the app and its API). Paths of the app's own routes (e.g. <c>/instanzen</c>) get <c>index.html</c>.
/// </summary>
internal static partial class WebAppHosting
{
    public const string SpaPathConstraint = "spapath";

    /// <summary>Path prefixes that belong to the server and never fall back to <c>index.html</c>.</summary>
    private static readonly string[] ServerPrefixes = ["api", "internal", "health", "openapi", ".well-known"];

    public static IServiceCollection AddWebApp(this IServiceCollection services) =>
        services.Configure<RouteOptions>(o => o.SetParameterPolicy<SpaPathRouteConstraint>(SpaPathConstraint));

    public static IApplicationBuilder UseWebAppFiles(this IApplicationBuilder app) =>
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
                context.Context.Response.Headers[HeaderNames.CacheControl] = CacheControlFor(context.File.Name),
        });

    public static IEndpointConventionBuilder MapWebAppFallback(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapFallbackToFile($"{{*path:{SpaPathConstraint}:nonfile}}", "index.html", new StaticFileOptions
        {
            OnPrepareResponse = context => context.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache",
        });

    /// <summary>
    /// Files with a content hash in their name (Angular build output) never change: cache for a year.
    /// Everything else (index.html, ngsw.json, the service worker, icons, translations) is revalidated.
    /// </summary>
    public static string CacheControlFor(string fileName) =>
        HashedFileName().IsMatch(fileName) ? "public, max-age=31536000, immutable" : "no-cache";

    /// <summary>True for app routes; false for server paths such as <c>/api/…</c>, which answer 404 instead.</summary>
    public static bool IsSpaPath(string? path)
    {
        var trimmed = (path ?? string.Empty).TrimStart('/');
        var first = trimmed.Split('/', 2)[0];
        return !ServerPrefixes.Contains(first, StringComparer.OrdinalIgnoreCase);
    }

    // Angular's 8-character hash (letters, digits, "_"); at least one non-lowercase character, so that
    // plain words like "-maskable.png" do not count as hashes.
    [GeneratedRegex(@"-(?=[A-Za-z0-9_]*[A-Z0-9_])[A-Za-z0-9_]{8}\.(js|css|woff2?|png|svg)$", RegexOptions.CultureInvariant)]
    private static partial Regex HashedFileName();

    private sealed class SpaPathRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection) =>
            IsSpaPath(values.TryGetValue(routeKey, out var value) ? value as string : null);
    }
}
