using CompanyApi.Application.Persistence;
using CompanyApi.Application.Services;
using CompanyApi.Infrastructure.Persistence;
using CompanyApi.Infrastructure.Security;
using CompanyApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyApi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers everything the application needs from the outside world: where data is stored, how passwords
    /// and tokens are produced, and how a company name is compared with its website.
    /// Application only knows the interfaces; this is the one place that picks the implementations.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Security ---------------------------------------------------------------------------------------
        // JWT settings come from the "Jwt" section (key, issuer, audience, lifetime) of appsettings / environment.
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        // --- Relevance: does the website's own page mention the company? -----------------------------------
        // Off by default because it makes outbound HTTP requests. Turn it on with Relevance:InspectContent=true.
        // When off, a do-nothing checker is registered so the service never has to ask whether it exists.
        if (configuration.GetValue<bool>("Relevance:InspectContent"))
            services.AddHttpClient<IContentRelevanceChecker, HttpContentRelevanceChecker>(c => c.DefaultRequestHeaders.UserAgent.ParseAdd("CompanyApi/1.0"))
                // The URL is typed by a user, so the connection guard refuses private and loopback addresses (SSRF).
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { ConnectCallback = HttpContentRelevanceChecker.ConnectToPublicAddress, MaxResponseHeadersLength = 32 });
        else
            services.AddSingleton<IContentRelevanceChecker, NullContentRelevanceChecker>();

        // --- Data source -------------------------------------------------------------------------------------
        // THE place that decides where data lives. The services never see the provider: they use IAppDbContext.
        //
        // Today: the EF Core in-memory provider (no database; data is lost when the process stops).
        // To change the data source, replace the UseInMemoryDatabase call below with one of these, keeping the rest:
        //
        //   options.UseSqlServer(configuration.GetConnectionString("Default"));   // package Microsoft.EntityFrameworkCore.SqlServer
        //   options.UseNpgsql(configuration.GetConnectionString("Default"));      // package Npgsql.EntityFrameworkCore.PostgreSQL
        //   options.UseSqlite(configuration.GetConnectionString("Default"));      // package Microsoft.EntityFrameworkCore.Sqlite
        //
        // then add "ConnectionStrings:Default" to the configuration and create the schema with migrations
        // (dotnet ef migrations add Initial --project CompanyApi.Infrastructure --startup-project CompanyApi.Api).
        // The unique indexes on Company.Domain and User.Email are already in the model and start being enforced.
        //
        // The root below keeps one application instance's in-memory data separate from any other instance
        // running in the same process (for example the host created by each integration test).
        services.AddSingleton(new InMemoryDatabaseRoot());
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseInMemoryDatabase("company-directory", sp.GetRequiredService<InMemoryDatabaseRoot>()));

        // What the services ask for is the interface; it resolves to the DbContext of the current request (scoped).
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // --- Stateless helpers (one instance for the whole application) -------------------------------------
        return services
            .AddSingleton<IRelevanceScorer, DomainRelevanceScorer>()   // name vs. domain comparison
            .AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>()     // PBKDF2 password hashes
            .AddSingleton<ITokenService, JwtTokenService>();           // signs the JWT returned on login
    }
}
