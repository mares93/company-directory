using CompanyApi.Application.Dtos;
using CompanyApi.Application.Services;
using CompanyApi.Application.Validation;
using CompanyApi.Infrastructure.Persistence;
using CompanyApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CompanyApi.Tests;

// The service runs against the real DbContext with the in-memory provider (one isolated store per test),
// and a fixed scorer / content checker so each test controls the relevance outcome.
public class CompanyServiceTests
{
    private class FixedScorer(double score) : IRelevanceScorer
    {
        public double Score(string text, string domain) => score;
    }

    private class FixedContentChecker(double score) : IContentRelevanceChecker
    {
        public Task<double> ScoreAsync(string name, Uri url, CancellationToken ct = default) => Task.FromResult(score);
    }

    private static (CompanyService Service, AppDbContext Db) Build(double score, double contentScore = 0, IRelevanceScorer? scorer = null)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new CompanyService(db, scorer ?? new FixedScorer(score), new FixedContentChecker(contentScore),
            new CreateCompanyRequestValidator(), new PageRequestValidator(), NullLogger<CompanyService>.Instance);
        return (service, db);
    }

    [Fact]
    public async Task Relevant_company_is_stored_with_its_creator()
    {
        var (service, db) = Build(1);
        await service.CreateAsync(new CreateCompanyRequest("Acme", "https://www.acme.com"), "tester@example.com");

        var stored = Assert.Single(db.Companies);
        Assert.Equal("acme.com", stored.Domain);
        Assert.Equal("tester@example.com", stored.CreatedBy);
    }

    [Fact]
    public async Task Irrelevant_company_is_rejected_and_not_stored()
    {
        var (service, db) = Build(0.1);
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCompanyRequest("Acme", "https://www.acme.com"), "tester@example.com"));
        Assert.Empty(db.Companies);
    }

    [Theory]
    [InlineData("", "https://www.acme.com", "Company name is required.")]
    [InlineData("ab", "https://www.acme.com", "at least 3 characters")]
    [InlineData("Acme", "not-a-url", "valid absolute URL")]
    [InlineData("Acme", "ftp://acme.com", "valid absolute URL")]
    public async Task Invalid_input_is_rejected_by_validator_with_clear_message(string name, string url, string expected)
    {
        var (service, db) = Build(1);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateCompanyRequest(name, url), "tester@example.com"));
        Assert.Contains(expected, ex.Message);
        Assert.Empty(db.Companies);
    }

    [Fact]
    public async Task Page_content_can_rescue_a_name_that_does_not_match_the_domain()
    {
        var (service, db) = Build(score: 0.1, contentScore: 1);
        await service.CreateAsync(new CreateCompanyRequest("Alphabet", "https://abc.xyz"), "tester@example.com");
        Assert.Single(db.Companies);
    }

    [Fact]
    public async Task Duplicate_domain_throws_Conflict()
    {
        var (service, db) = Build(1);
        var request = new CreateCompanyRequest("Acme", "https://www.acme.com");
        await service.CreateAsync(request, "tester@example.com");
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request, "tester@example.com"));
        Assert.Single(db.Companies);
    }

    private static async Task<CompanyService> BuildWithCompanies(params string[] names)
    {
        var (service, _) = Build(1);
        foreach (var n in names)
            await service.CreateAsync(new CreateCompanyRequest(n, $"https://www.{n.ToLowerInvariant()}.com"), "tester@example.com");
        return service;
    }

    [Fact]
    public async Task GetAll_pages_and_reports_total()
    {
        var service = await BuildWithCompanies("Delta", "Alpha", "Charlie", "Bravo", "Echo");

        var page2 = await service.GetAllAsync(null, null, new PageRequest(Page: 2, PageSize: 2, Sort: "name"));
        Assert.Equal(5, page2.Total);
        Assert.Equal(["Charlie", "Delta"], page2.Items.Select(c => c.Name));

        var beyond = await service.GetAllAsync(null, null, new PageRequest(Page: 9, PageSize: 2));
        Assert.Empty(beyond.Items);
        Assert.Equal(5, beyond.Total);

        // (Page - 1) * PageSize overflows int here; it used to wrap around and return the first page.
        var huge = await service.GetAllAsync(null, null, new PageRequest(Page: int.MaxValue, PageSize: 100));
        Assert.Empty(huge.Items);
    }

    [Fact]
    public async Task GetAll_without_an_order_keeps_creation_order_and_newest_reverses_it()
    {
        var service = await BuildWithCompanies("Delta", "Alpha", "Charlie");

        var created = await service.GetAllAsync(null, null, new PageRequest());
        Assert.Equal(["Delta", "Alpha", "Charlie"], created.Items.Select(c => c.Name));

        var newest = await service.GetAllAsync(null, null, new PageRequest(Sort: "newest"));
        Assert.Equal(["Charlie", "Alpha", "Delta"], newest.Items.Select(c => c.Name));
    }

    [Fact]
    public async Task GetAll_filters_by_name_ignoring_case()
    {
        var service = await BuildWithCompanies("Acme", "Beta", "Acmetools");

        var result = await service.GetAllAsync("ACME", null, new PageRequest(Sort: "name"));
        Assert.Equal(["Acme", "Acmetools"], result.Items.Select(c => c.Name));
        Assert.Equal(2, result.Total);
    }

    [Theory]
    [InlineData("acme.com")]
    [InlineData("www.acme")]
    [InlineData("https://")]
    [InlineData("HTTPS://WWW.ACME.COM")]
    public async Task Domain_filter_also_matches_the_full_url(string filter)
    {
        var service = await BuildWithCompanies("Acme");
        var result = await service.GetAllAsync(null, filter, new PageRequest());
        Assert.Equal(["Acme"], result.Items.Select(c => c.Name));
    }

    [Fact]
    public async Task Search_filters_by_domain_and_sorts_on_request()
    {
        // Real scorer here: the fixed one would match every company.
        var (service, _) = Build(1, scorer: new DomainRelevanceScorer());
        foreach (var n in new[] { "Acme", "Acmetools", "Other" })
            await service.CreateAsync(new CreateCompanyRequest(n, $"https://www.{n.ToLowerInvariant()}.com"), "tester@example.com");

        var byName = await service.SearchAsync("acme", null, new PageRequest(Sort: "name"));
        Assert.Equal(["Acme", "Acmetools"], byName.Items.Select(r => r.Company.Name));

        var byDomain = await service.SearchAsync("acme", "acmetools", new PageRequest());
        Assert.Equal(["Acmetools"], byDomain.Items.Select(r => r.Company.Name));
    }

    [Theory]
    [InlineData(0, 10, null)]
    [InlineData(1, 0, null)]
    [InlineData(1, 101, null)]
    [InlineData(1, 10, "price")]
    public async Task Invalid_paging_throws_validation_error(int page, int size, string? sort)
    {
        var (service, _) = Build(1);
        await Assert.ThrowsAsync<ValidationException>(() => service.GetAllAsync(null, null, new PageRequest(page, size, sort)));
    }

    [Fact]
    public async Task GetById_returns_the_company()
    {
        var (service, _) = Build(1);
        var created = await service.CreateAsync(new CreateCompanyRequest("Acme", "https://www.acme.com"), "tester@example.com");

        var found = await service.GetByIdAsync(created.Id);

        Assert.Equal(created, found);
    }

    [Fact]
    public async Task Missing_company_throws_NotFound()
    {
        var (service, _) = Build(1);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }
}
