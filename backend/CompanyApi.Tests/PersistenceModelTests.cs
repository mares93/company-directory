using CompanyApi.Domain;
using CompanyApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Tests;

// The in-memory provider ignores unique indexes, so the model itself is checked: a relational provider
// (SQL Server, PostgreSQL...) will enforce them, and they are what makes duplicates impossible under concurrency.
public class PersistenceModelTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Company_domain_is_unique()
    {
        using var db = NewDb();
        var indexes = db.Model.FindEntityType(typeof(Company))!.GetIndexes();
        Assert.Contains(indexes, i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([nameof(Company.Domain)]));
    }

    [Fact]
    public void User_email_is_unique()
    {
        using var db = NewDb();
        var indexes = db.Model.FindEntityType(typeof(User))!.GetIndexes();
        Assert.Contains(indexes, i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual([nameof(User.Email)]));
    }

    [Fact]
    public void Column_lengths_match_what_validation_allows()
    {
        using var db = NewDb();
        var company = db.Model.FindEntityType(typeof(Company))!;
        Assert.Equal(200, company.FindProperty(nameof(Company.Name))!.GetMaxLength());
        Assert.Equal(2048, company.FindProperty(nameof(Company.WebsiteUrl))!.GetMaxLength());
        Assert.Equal(254, db.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.Email))!.GetMaxLength());
    }

    [Fact]
    public async Task A_stored_company_comes_back_with_every_field()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options;
        var company = new Company(Guid.NewGuid(), "Acme", "https://acme.com/", "acme.com", "a@b.com", DateTime.UtcNow);

        await using (var writer = new AppDbContext(options))
        {
            writer.Companies.Add(company);
            await writer.SaveChangesAsync();
        }

        await using var reader = new AppDbContext(options); // a new context proves it was really stored
        Assert.Equal(company, await reader.Companies.SingleAsync());
    }
}
