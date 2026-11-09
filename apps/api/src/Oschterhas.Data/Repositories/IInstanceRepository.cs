using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

/// <summary>Read access to instances. Kept thin: queries only, no business rules.</summary>
public interface IInstanceRepository
{
    Task<Guid?> FindIdBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Only custom domains with status <see cref="CustomDomainStatus.Verified"/> are found.</summary>
    Task<Guid?> FindIdByVerifiedCustomDomainAsync(string domain, CancellationToken cancellationToken);

    /// <summary>Instance with its hares ordered by number, read-only (no tracking).</summary>
    Task<Instance?> GetWithHaresAsync(Guid id, CancellationToken cancellationToken);
}
