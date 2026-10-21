using CompanyApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Application.Persistence;

/// <summary>
/// What the application needs from storage. The DbContext in Infrastructure implements it,
/// so the provider (in-memory today, SQL Server, PostgreSQL... later) is chosen there and nowhere else.
/// </summary>
public interface IAppDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
