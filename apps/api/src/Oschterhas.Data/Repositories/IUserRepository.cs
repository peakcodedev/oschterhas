using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

public interface IUserRepository
{
    /// <summary>Tracked user matched by issuer + subject (never by e-mail alone).</summary>
    Task<AppUser?> FindByIdentityAsync(string issuer, string subject, CancellationToken cancellationToken);

    /// <summary>Tracked user.</summary>
    Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>All users with the ids of the instances they organize, read-only.</summary>
    Task<IReadOnlyList<AppUser>> ListAsync(CancellationToken cancellationToken);

    void Add(AppUser user);

    /// <exception cref="UniqueConstraintException">E.g. the identity already exists.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
