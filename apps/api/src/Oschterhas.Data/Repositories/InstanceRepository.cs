using Microsoft.EntityFrameworkCore;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

internal sealed class InstanceRepository(AppDbContext db) : IInstanceRepository
{
    public Task<Guid?> FindIdBySlugAsync(string slug, CancellationToken cancellationToken) =>
        db.Instances
            .Where(i => i.Slug == slug)
            .Select(i => (Guid?)i.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Guid?> FindIdByVerifiedCustomDomainAsync(string domain, CancellationToken cancellationToken) =>
        db.Instances
            .Where(i => i.CustomDomain == domain && i.CustomDomainStatus == CustomDomainStatus.Verified)
            .Select(i => (Guid?)i.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Instance?> GetWithHaresAsync(Guid id, CancellationToken cancellationToken) =>
        db.Instances
            .AsNoTracking()
            .Include(i => i.Hares.OrderBy(h => h.Number))
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Instance>> ListAsync(Guid? organizerId, CancellationToken cancellationToken) =>
        await db.Instances
            .AsNoTracking()
            .AsSplitQuery()
            .Where(i => organizerId == null || i.Organizers.Any(o => o.UserId == organizerId))
            .Include(i => i.Hares)
            .Include(i => i.Organizers).ThenInclude(o => o.User)
            .OrderByDescending(i => i.EventDate).ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);

    public Task<Instance?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        db.Instances
            .AsSplitQuery()
            .Include(i => i.Organizers).ThenInclude(o => o.User)
            .Include(i => i.Hares)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        db.Instances.AnyAsync(i => i.Slug == slug, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Instances.AnyAsync(i => i.Id == id, cancellationToken);

    public Task<bool> IsOrganizerAsync(Guid instanceId, Guid userId, CancellationToken cancellationToken) =>
        db.InstanceOrganizers.AnyAsync(o => o.InstanceId == instanceId && o.UserId == userId, cancellationToken);

    public Task<bool> CustomDomainTakenAsync(string domain, Guid exceptInstanceId, CancellationToken cancellationToken) =>
        db.Instances.AnyAsync(i => i.CustomDomain == domain && i.Id != exceptInstanceId, cancellationToken);

    public Task<string?> FindCustomDomainTokenAsync(string domain, CancellationToken cancellationToken) =>
        db.Instances
            .Where(i => i.CustomDomain == domain && i.CustomDomainStatus != CustomDomainStatus.None)
            .Select(i => i.CustomDomainToken)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<Instance>> ListWithCustomDomainForUpdateAsync(CancellationToken cancellationToken) =>
        db.Instances
            .Where(i => i.CustomDomain != null && i.CustomDomainStatus != CustomDomainStatus.None)
            .ToListAsync(cancellationToken);

    public void Add(Instance instance) => db.Instances.Add(instance);

    public void Remove(Instance instance) => db.Instances.Remove(instance);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveAsync(cancellationToken);
}
