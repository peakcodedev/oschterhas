using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Configurations;

internal sealed class HareConfiguration : IEntityTypeConfiguration<Hare>
{
    public void Configure(EntityTypeBuilder<Hare> builder)
    {
        builder.ToTable("hares");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name).HasMaxLength(FieldLengths.HareName).IsRequired(false);
        builder.Property(h => h.Description).HasMaxLength(FieldLengths.HareDescription);
        builder.Property(h => h.SourceWaypointId).HasMaxLength(FieldLengths.SourceWaypointId);
        builder.Property(h => h.FoundBy).HasMaxLength(FieldLengths.Nickname);

        // Npgsql maps a uint row version to the xmin system column (optimistic concurrency).
        builder.Property(h => h.Version).IsRowVersion().HasColumnName("xmin");

        builder.HasIndex(h => new { h.InstanceId, h.Number }).IsUnique();
        builder.HasIndex(h => new { h.InstanceId, h.SourceWaypointId }).IsUnique();

        builder.HasMany(h => h.FoundEvents)
            .WithOne(e => e.Hare)
            .HasForeignKey(e => e.HareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
