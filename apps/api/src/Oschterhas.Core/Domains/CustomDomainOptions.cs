namespace Oschterhas.Core.Domains;

public sealed class CustomDomainOptions
{
    public const string SectionName = "CustomDomains";

    /// <summary>CNAME target shown in the DNS instructions, e.g. <c>custom.oschterhas.li</c>.</summary>
    public string DnsTarget { get; set; } = "custom.oschterhas.li";

    /// <summary>Port of the HTTP verification request; null = 80. Locally the dev server port.</summary>
    public int? VerificationPort { get; set; }

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan PendingCheckInterval { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan VerifiedCheckInterval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Failed checks in a row after which a verified domain is no longer served.</summary>
    public int MaxFailedChecks { get; set; } = 3;

    /// <summary>Development only: accept <c>*.localhost</c> domains (resolved to loopback).</summary>
    public bool AllowLocalhostDomains { get; set; }

    /// <summary>Development only: allow verification requests to private/loopback addresses (otherwise SSRF protection).</summary>
    public bool AllowPrivateAddresses { get; set; }
}
