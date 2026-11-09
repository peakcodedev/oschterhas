namespace Oschterhas.Data.Entities;

/// <summary>One search, reachable at <c>{Slug}.oschterhas.li</c> and optionally at a verified custom domain.</summary>
public class Instance
{
    public Guid Id { get; set; }

    /// <summary>Subdomain, e.g. <c>duernten</c>.</summary>
    public required string Slug { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public DateOnly? EventDate { get; set; }

    public InstanceStatus Status { get; set; } = InstanceStatus.Draft;

    /// <summary>Normalized host name, e.g. <c>ostern.ceviduernten.ch</c>. Served only while <see cref="CustomDomainStatus"/> is Verified.</summary>
    public string? CustomDomain { get; set; }

    public CustomDomainStatus CustomDomainStatus { get; set; } = CustomDomainStatus.None;

    public string? CustomDomainToken { get; set; }

    public DateTimeOffset? CustomDomainVerifiedAt { get; set; }

    public DateTimeOffset? CustomDomainLastCheckAt { get; set; }

    public int CustomDomainFailedChecks { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<InstanceOrganizer> Organizers { get; set; } = [];

    public List<Hare> Hares { get; set; } = [];
}
