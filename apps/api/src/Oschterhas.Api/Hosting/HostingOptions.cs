namespace Oschterhas.Api.Hosting;

/// <summary>Host names the platform runs on. Instances live at <c>{slug}.{BaseDomain}</c>.</summary>
public sealed class HostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>E.g. <c>oschterhas.li</c>, locally <c>oschterhas.localhost</c>.</summary>
    public string BaseDomain { get; set; } = string.Empty;

    /// <summary>Subdomain of the administration area, e.g. <c>admin</c>.</summary>
    public string AdminSubdomain { get; set; } = "admin";

    public string AdminHost => $"{AdminSubdomain}.{BaseDomain}";
}
