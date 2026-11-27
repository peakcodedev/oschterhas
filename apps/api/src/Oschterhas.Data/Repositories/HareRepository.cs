using Microsoft.EntityFrameworkCore;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

internal sealed class HareRepository(AppDbContext db) : IHareRepository
{
    public Task<Hare?> FindForUpdateAsync(Guid instanceId, Guid hareId, CancellationToken cancellationToken) =>
        db.Hares
            .Include(h => h.Instance)
            .FirstOrDefaultAsync(h => h.Id == hareId && h.InstanceId == instanceId, cancellationToken);

    public Task<List<Hare>> ListForUpdateAsync(Guid instanceId, CancellationToken cancellationToken) =>
        db.Hares
            .Where(h => h.InstanceId == instanceId)
            .OrderBy(h => h.Number)
            .ToListAsync(cancellationToken);

    public void Add(Hare hare) => db.Hares.Add(hare);

    public void Remove(Hare hare) => db.Hares.Remove(hare);

    public void AddFoundEvent(FoundEvent foundEvent) => db.FoundEvents.Add(foundEvent);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveAsync(cancellationToken);
}
