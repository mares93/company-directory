using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CompanyApi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public static IWebHostBuilder WithAuthLimit(IWebHostBuilder builder, int perMinute) =>
        builder.ConfigureAppConfiguration((_, c) =>
            c.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimit:AuthPerMinute"] = perMinute.ToString() }));

    // Tests create many accounts from one address, so the limit is lifted here and tested on its own below.
    protected override void ConfigureWebHost(IWebHostBuilder builder) => WithAuthLimit(builder, 1000);
}
