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
}
