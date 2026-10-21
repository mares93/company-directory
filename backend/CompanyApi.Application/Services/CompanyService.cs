using System.Linq.Expressions;
using CompanyApi.Application.Dtos;
using CompanyApi.Application.Persistence;
using CompanyApi.Application.Validation;
using CompanyApi.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompanyApi.Application.Services;

public class CompanyService(
    IAppDbContext db,
    IRelevanceScorer scorer,
    IContentRelevanceChecker contentChecker,
    IValidator<CreateCompanyRequest> validator,
    IValidator<PageRequest> pageValidator,
    ILogger<CompanyService> logger) : ICompanyService
{
    private const double MinRelevance = 0.5;

    // One projection for every read: the provider turns it into the SELECT, and it is compiled once for single objects.
    private static readonly Expression<Func<Company, CompanyResponse>> ToResponseExpression =
        c => new CompanyResponse(c.Id, c.Name, c.WebsiteUrl, c.Domain, c.CreatedBy, c.CreatedAtUtc);

    private static readonly Func<Company, CompanyResponse> ToResponse = ToResponseExpression.Compile();

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, string createdBy)
    {
        validator.ValidateOrThrow(request);
        var name = request.Name!.Trim();
        var uri = new Uri(request.WebsiteUrl!.Trim());

        var domain = uri.Host.StartsWith("www.") ? uri.Host[4..] : uri.Host;
        var relevance = scorer.Score(name, domain);
        // Second chance: the page itself may name the company even if the domain does not (e.g. a brand on another domain).
        if (relevance < MinRelevance)
            relevance = Math.Max(relevance, await contentChecker.ScoreAsync(name, uri));
        if (relevance < MinRelevance)
        {
            logger.LogWarning("Rejected company {Name}: not relevant to {Domain} (score {Score:F2})", name, domain, relevance);
            throw new ValidationException($"Company name '{name}' is not relevant to the website '{domain}'.");
        }

        // The explicit check gives a clear error; the unique index on Domain is what holds under concurrent requests.
        if (await db.Companies.AnyAsync(c => c.Domain == domain))
            throw DuplicateDomain(domain);

        var company = new Company(Guid.NewGuid(), name, uri.ToString(), domain, createdBy, DateTime.UtcNow);
        db.Companies.Add(company);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A concurrent request took the domain between the check and the insert.
            if (await db.Companies.AnyAsync(c => c.Domain == domain)) throw DuplicateDomain(domain);
            throw;
        }

        logger.LogInformation("Created company {Id} ({Name}, {Domain}) by {CreatedBy}", company.Id, name, domain, createdBy);
        return ToResponse(company);
    }

    public async Task<CompanyResponse> GetByIdAsync(Guid id) =>
        await db.Companies.AsNoTracking().Where(c => c.Id == id).Select(ToResponseExpression).FirstOrDefaultAsync()
        ?? throw new NotFoundException($"Company {id} not found.");

    // Filtering, ordering and paging all run in the data source (SELECT ... WHERE ... ORDER BY ... OFFSET), only the page is read.
    public async Task<PagedResponse<CompanyResponse>> GetAllAsync(string? name, string? domain, PageRequest paging)
    {
        pageValidator.ValidateOrThrow(paging);

        var matches = db.Companies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var text = name.Trim().ToLower();
            matches = matches.Where(c => c.Name.ToLower().Contains(text));
        }
        matches = FilterByDomain(matches, domain);

        // Without an explicit order the creation order is kept ("relevance" has no meaning for a plain list).
        var ordered = paging.Sort?.ToLowerInvariant() switch
        {
            "name" => matches.OrderBy(c => c.Name),
            "newest" => matches.OrderByDescending(c => c.CreatedAtUtc),
            _ => matches.OrderBy(c => c.CreatedAtUtc)
        };

        var total = await matches.CountAsync();
        // long: (Page - 1) * PageSize overflows int for huge pages and would wrap to a negative skip.
        var skip = (long)(paging.Page - 1) * paging.PageSize;
        IReadOnlyList<CompanyResponse> items = skip >= total
            ? []
            : await ordered.ThenBy(c => c.Id).Skip((int)skip).Take(paging.PageSize).Select(ToResponseExpression).ToListAsync();

        return new PagedResponse<CompanyResponse>(items, total, paging.Page, paging.PageSize);
    }

    // shortcut: relevance is computed in C# (DomainRelevanceScorer), so candidates are read and scored in memory, O(n) per search.
    // Upgrade to a normalized column or full-text search in the data source when the table gets big.
    public async Task<PagedResponse<CompanySearchResult>> SearchAsync(string query, string? domain, PageRequest paging)
    {
        pageValidator.ValidateOrThrow(paging);

        var candidates = await FilterByDomain(db.Companies.AsNoTracking(), domain).ToListAsync();
        var scored = candidates
            .Select(c => (Company: c, Score: Math.Max(scorer.Score(query, c.Domain), NameScore(query, c.Name))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Company.Name); // default order: relevance
        var ordered = OrderBy(scored, paging.Sort, x => x.Company).ToList();
        return PagedResponse<CompanySearchResult>.From(ordered, paging,
            x => new CompanySearchResult(ToResponse(x.Company), Math.Round(x.Score, 2)));
    }

    // The filter looks in the domain and in the full URL, so "acme.com", "www.acme" and "https://" all match.
    private static IQueryable<Company> FilterByDomain(IQueryable<Company> companies, string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return companies;
        var text = domain.Trim().ToLower();
        return companies.Where(c => c.Domain.ToLower().Contains(text) || c.WebsiteUrl.ToLower().Contains(text));
    }

    // "name" and "newest" override the incoming order; anything else keeps it.
    private static IEnumerable<T> OrderBy<T>(IEnumerable<T> items, string? sort, Func<T, Company> company) =>
        sort?.ToLowerInvariant() switch
        {
            "name" => items.OrderBy(x => company(x).Name, StringComparer.OrdinalIgnoreCase),
            "newest" => items.OrderByDescending(x => company(x).CreatedAtUtc),
            _ => items
        };

    // Name match: exact 1, prefix 0.9, substring 0.7.
    private static double NameScore(string query, string name)
    {
        var q = query.Trim();
        if (name.Equals(q, StringComparison.OrdinalIgnoreCase)) return 1;
        if (name.StartsWith(q, StringComparison.OrdinalIgnoreCase)) return 0.9;
        return name.Contains(q, StringComparison.OrdinalIgnoreCase) ? 0.7 : 0;
    }

    private static ConflictException DuplicateDomain(string domain) =>
        new($"A company with domain '{domain}' is already registered.");
}
