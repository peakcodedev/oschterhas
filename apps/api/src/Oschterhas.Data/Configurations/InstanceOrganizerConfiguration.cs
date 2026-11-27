using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Configurations;

internal sealed class InstanceOrganizerConfiguration : IEntityTypeConfiguration<InstanceOrganizer>
{
    public void Configure(EntityTypeBuilder<InstanceOrganizer> builder)
    {
        builder.ToTable("instance_organizers");
        builder.HasKey(o => new { o.InstanceId, o.UserId });

        builder.HasOne(o => o.Instance)
            .WithMany(i => i.Organizers)
            .HasForeignKey(o => o.InstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.User)
            .WithMany(u => u.Instances)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
