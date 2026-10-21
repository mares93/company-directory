using CompanyApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyApi.Infrastructure.Persistence;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(254);
        builder.Property(u => u.PasswordHash).HasMaxLength(256);

        // Emails are stored lowercase, so a plain unique index is case-insensitive in practice.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
