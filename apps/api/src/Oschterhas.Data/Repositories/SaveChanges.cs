using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Oschterhas.Data.Repositories;

internal static class SaveChanges
{
    /// <summary>SaveChanges with database errors translated into exceptions Core can handle without EF/Npgsql.</summary>
    public static async Task SaveAsync(this AppDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw new UniqueConstraintException(pg.ConstraintName, ex);
        }
    }
}
