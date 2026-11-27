using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

public interface IHareRepository
{
    /// <summary>Tracked hare including its instance; null if it does not exist or belongs to another instance.</summary>
    Task<Hare?> FindForUpdateAsync(Guid instanceId, Guid hareId, CancellationToken cancellationToken);

    /// <summary>All hares of the instance ordered by number, tracked (for imports and edits).</summary>
    Task<List<Hare>> ListForUpdateAsync(Guid instanceId, CancellationToken cancellationToken);

    void Add(Hare hare);

    void Remove(Hare hare);

    void AddFoundEvent(FoundEvent foundEvent);

    /// <exception cref="ConcurrencyConflictException">The hare was changed concurrently.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
