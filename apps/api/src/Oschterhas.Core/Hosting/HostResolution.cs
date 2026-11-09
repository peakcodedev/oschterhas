namespace Oschterhas.Core.Hosting;

public enum HostKind
{
    /// <summary>Host is not served (unknown subdomain, unverified custom domain, invalid name).</summary>
    Unknown,

    /// <summary>The base domain itself (or www): info page.</summary>
    Landing,

    /// <summary><c>admin.{BaseDomain}</c>: administration.</summary>
    Admin,

    /// <summary>A search: <c>{slug}.{BaseDomain}</c> or a verified custom domain.</summary>
    Instance,
}

public sealed record HostResolution(HostKind Kind, Guid? InstanceId = null)
{
    public static readonly HostResolution Unknown = new(HostKind.Unknown);

    public static readonly HostResolution Landing = new(HostKind.Landing);

    public static readonly HostResolution Admin = new(HostKind.Admin);

    public static HostResolution ForInstance(Guid id) => new(HostKind.Instance, id);
}
