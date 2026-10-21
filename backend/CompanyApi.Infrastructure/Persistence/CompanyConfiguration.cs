using CompanyApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyApi.Infrastructure.Persistence;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.WebsiteUrl).HasMaxLength(2048);
        builder.Property(c => c.Domain).HasMaxLength(255);
        builder.Property(c => c.CreatedBy).HasMaxLength(254);

        // One company per domain. Relational providers enforce this; the in-memory provider does not,
        // which is why the service also checks explicitly before inserting.
        builder.HasIndex(c => c.Domain).IsUnique();
    }
}
