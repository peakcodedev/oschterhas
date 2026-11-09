using Microsoft.EntityFrameworkCore;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<AppUser?> FindByIdentityAsync(string issuer, string subject, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.IdentityIssuer == issuer && u.IdentitySubject == subject, cancellationToken);

    public Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AppUser>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users
            .AsNoTracking()
            .Include(u => u.Instances)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);

    public void Add(AppUser user) => db.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveAsync(cancellationToken);
}
