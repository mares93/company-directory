using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CompanyApi.Application.Dtos;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CompanyApi.Tests;

public class AuthHardeningTests
{
    private const string RealKey = "dev-only-secret-key-change-me-0123456789abcdef"; // appsettings.json

    private static string Token(string key, string audience = "CompanyApi.Client", bool expired = false)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "CompanyApi",
            Audience = audience,
            NotBefore = expired ? now.AddHours(-2) : now,
            Expires = expired ? now.AddHours(-1) : now.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["email"] = "forger@example.com" },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        });
    }

    private static async Task<HttpStatusCode> ListWith(string token)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.GetAsync("/api/companies")).StatusCode;
    }

    [Fact]
    public async Task Control_a_correctly_signed_token_is_accepted() =>
        Assert.Equal(HttpStatusCode.OK, await ListWith(Token(RealKey)));

    [Fact]
    public async Task Token_signed_with_another_key_is_rejected() =>
        Assert.Equal(HttpStatusCode.Unauthorized, await ListWith(Token("another-secret-key-another-secret-key-0000")));

    [Fact]
    public async Task Token_for_another_audience_is_rejected() =>
        Assert.Equal(HttpStatusCode.Unauthorized, await ListWith(Token(RealKey, audience: "someone-else")));

    [Fact]
    public async Task Expired_token_is_rejected() =>
        Assert.Equal(HttpStatusCode.Unauthorized, await ListWith(Token(RealKey, expired: true)));

    [Fact]
    public async Task Auth_endpoints_are_rate_limited_per_client()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(b => ApiFactory.WithAuthLimit(b, 3));
        var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            statuses.Add((await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@example.com", "wrong-password"))).StatusCode);

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], statuses);
    }

    [Fact]
    public async Task Register_rejects_an_oversized_email()
    {
        using var factory = new ApiFactory();
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(new string('a', 300) + "@example.com", "password123"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
