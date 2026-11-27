using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Configurations;

internal sealed class FoundEventConfiguration : IEntityTypeConfiguration<FoundEvent>
{
    public void Configure(EntityTypeBuilder<FoundEvent> builder)
    {
        builder.ToTable("found_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(FieldLengths.EnumValue);
        builder.Property(e => e.Nickname).HasMaxLength(FieldLengths.Nickname);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
