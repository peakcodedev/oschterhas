namespace Oschterhas.Core.Hosting;

/// <summary>Host names the platform runs on. Instances live at <c>{slug}.{BaseDomain}</c>.</summary>
public sealed class HostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>E.g. <c>oschterhas.li</c>, locally <c>oschterhas.localhost</c>.</summary>
    public string BaseDomain { get; set; } = string.Empty;

    /// <summary>Subdomain of the administration area, e.g. <c>admin</c>.</summary>
    public string AdminSubdomain { get; set; } = "admin";

    /// <summary>Scheme of public links and QR codes (<c>https</c>; locally <c>http</c>).</summary>
    public string PublicScheme { get; set; } = "https";

    /// <summary>Port of public links; null for the default port (locally the dev server port).</summary>
    public int? PublicPort { get; set; }

    public string AdminHost => $"{AdminSubdomain}.{BaseDomain}";

    /// <summary>Public link to a host, built from configuration (never from the request's Host header).</summary>
    public string PublicUrl(string host) => PublicPort is { } port ? $"{PublicScheme}://{host}:{port}" : $"{PublicScheme}://{host}";
}
