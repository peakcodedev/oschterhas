using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oschterhas.Data.Entities;

namespace Oschterhas.Data.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.IdentityIssuer).HasMaxLength(FieldLengths.IdentityIssuer);
        builder.Property(u => u.IdentitySubject).HasMaxLength(FieldLengths.IdentitySubject);
        builder.Property(u => u.Email).HasMaxLength(FieldLengths.Email);
        builder.Property(u => u.DisplayName).HasMaxLength(FieldLengths.DisplayName);
        builder.Property(u => u.Locale).HasMaxLength(FieldLengths.Locale);

        builder.HasIndex(u => new { u.IdentityIssuer, u.IdentitySubject }).IsUnique();
        builder.HasIndex(u => u.Email);
    }
}
