using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Configurations;

internal sealed class InstanceConfiguration : IEntityTypeConfiguration<Instance>
{
    public void Configure(EntityTypeBuilder<Instance> builder)
    {
        builder.ToTable("instances");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Slug).HasMaxLength(FieldLengths.Slug);
        builder.Property(i => i.Name).HasMaxLength(FieldLengths.InstanceName);
        builder.Property(i => i.Description).HasMaxLength(FieldLengths.InstanceDescription);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(FieldLengths.EnumValue);
        builder.Property(i => i.CustomDomain).HasMaxLength(FieldLengths.HostName);
        builder.Property(i => i.CustomDomainStatus).HasConversion<string>().HasMaxLength(FieldLengths.EnumValue);
        builder.Property(i => i.CustomDomainToken).HasMaxLength(FieldLengths.Token);

        builder.HasIndex(i => i.Slug).IsUnique();

        // PostgreSQL allows several NULLs in a unique index, so instances without a custom domain do not collide.
        builder.HasIndex(i => i.CustomDomain).IsUnique();

        builder.HasMany(i => i.Hares)
            .WithOne(h => h.Instance)
            .HasForeignKey(h => h.InstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
