using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

/// <summary>Access to instances. Kept thin: queries only, no business rules.</summary>
public interface IInstanceRepository
{
    Task<Guid?> FindIdBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Only custom domains with status <see cref="CustomDomainStatus.Verified"/> are found.</summary>
    Task<Guid?> FindIdByVerifiedCustomDomainAsync(string domain, CancellationToken cancellationToken);

    /// <summary>Instance with its hares ordered by number, read-only (no tracking).</summary>
    Task<Instance?> GetWithHaresAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Instances with hares and organizers, read-only. <paramref name="organizerId"/> null returns all,
    /// otherwise only the instances that user organizes.
    /// </summary>
    Task<IReadOnlyList<Instance>> ListAsync(Guid? organizerId, CancellationToken cancellationToken);

    /// <summary>Tracked instance with organizers (and their users) and hares.</summary>
    Task<Instance?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsOrganizerAsync(Guid instanceId, Guid userId, CancellationToken cancellationToken);

    void Add(Instance instance);

    void Remove(Instance instance);

    /// <exception cref="UniqueConstraintException">Slug or custom domain already taken.</exception>
    /// <exception cref="ConcurrencyConflictException">Changed concurrently.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
