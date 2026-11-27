using Microsoft.EntityFrameworkCore;
using Oschterhas.Common.Text;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Instance> Instances => Set<Instance>();

    public DbSet<InstanceOrganizer> InstanceOrganizers => Set<InstanceOrganizer>();

    public DbSet<Hare> Hares => Set<Hare>();

    public DbSet<FoundEvent> FoundEvents => Set<FoundEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplySnakeCaseNames(modelBuilder);
    }

    /// <summary>Uses snake_case for all columns, keys, foreign keys and indexes (tables are named explicitly).</summary>
    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(NamingConventions.ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(NamingConventions.ToSnakeCase(key.GetName()!));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(NamingConventions.ToSnakeCase(foreignKey.GetConstraintName()!));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(NamingConventions.ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }
}
