using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace Oschterhas.Api.Hosting;

/// <summary>Settings for running behind the reverse proxy (Caddy) with several API restarts.</summary>
public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    /// <summary>
    /// Networks of the reverse proxy whose X-Forwarded-For/-Proto headers are trusted (e.g. the Docker network).
    /// Empty: no forwarded headers are trusted, so clients cannot fake their IP (rate limits use the connection IP).
    /// </summary>
    public IList<string> TrustedNetworks { get; set; } = [];

    /// <summary>Directory for the ASP.NET data protection keys (session and antiforgery cookies); a persistent volume in production.</summary>
    public string? DataProtectionKeysDirectory { get; set; }
}

internal static class ProductionHosting
{
    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ReverseProxyOptions.SectionName).Get<ReverseProxyOptions>() ?? new ReverseProxyOptions();

        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            // The Host header is passed through unchanged by the proxy; only client IP and scheme are forwarded.
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            forwarded.ForwardLimit = 1;
            forwarded.KnownProxies.Clear();
            forwarded.KnownIPNetworks.Clear();
            foreach (var network in options.TrustedNetworks)
            {
                forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        var dataProtection = services.AddDataProtection().SetApplicationName("oschterhas");
        if (!string.IsNullOrWhiteSpace(options.DataProtectionKeysDirectory))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(options.DataProtectionKeysDirectory));
        }

        services.AddHsts(hsts =>
        {
            hsts.MaxAge = TimeSpan.FromDays(365);
            hsts.IncludeSubDomains = true;
        });

        return services;
    }
}
