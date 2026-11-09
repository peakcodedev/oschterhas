using Microsoft.EntityFrameworkCore;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

internal sealed class HareRepository(AppDbContext db) : IHareRepository
{
    public Task<Hare?> FindForUpdateAsync(Guid instanceId, Guid hareId, CancellationToken cancellationToken) =>
        db.Hares
            .Include(h => h.Instance)
            .FirstOrDefaultAsync(h => h.Id == hareId && h.InstanceId == instanceId, cancellationToken);

    public void AddFoundEvent(FoundEvent foundEvent) => db.FoundEvents.Add(foundEvent);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }
}
