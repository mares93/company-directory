using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CompanyApi.Application.Dtos;

namespace CompanyApi.Tests;

public class CompanyApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string NewEmail() => $"user{Guid.NewGuid():N}@example.com";

    private async Task<AuthResponse> Register(string email, string password = "password123")
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private async Task SignIn()
    {
        var auth = await Register(NewEmail());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }

    private async Task<HttpResponseMessage> Post(string? name, string? url, string? token = null)
    {
        token ??= (await Register(NewEmail())).Token;
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/companies") { Content = JsonContent.Create(new CreateCompanyRequest(name, url)) };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(req);
    }

    [Fact]
    public async Task Valid_company_returns_201_and_can_be_fetched()
    {
        var auth = await Register(NewEmail());
        var res = await Post("Acme Corp", "https://www.acme.com", auth.Token);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = (await res.Content.ReadFromJsonAsync<CompanyResponse>())!;
        Assert.Equal(auth.Email, created.CreatedBy);

        var get = await _client.GetAsync($"/api/companies/{created.Id}"); // after Register/Post only the Post carried a token
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        await SignIn();
        get = await _client.GetAsync($"/api/companies/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Theory]
    [InlineData("", "https://www.acme.com")]
    [InlineData("ab", "https://www.acme.com")]
    [InlineData("Acme", "not-a-url")]
    [InlineData("Acme", "ftp://acme.com")]
    [InlineData("Acme", "https://www.google.com")] // irrelevant
    public async Task Invalid_company_returns_400(string name, string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(name, url)).StatusCode);

    [Fact]
    public async Task Duplicate_domain_returns_409()
    {
        var token = (await Register(NewEmail())).Token;
        Assert.Equal(HttpStatusCode.Created, (await Post("Dup Corp", "https://www.dupcorp.com", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post("Dup Corp", "https://dupcorp.com", token)).StatusCode);
    }

    [Fact]
    public async Task Creating_company_without_token_returns_401()
    {
        var res = await _client.PostAsJsonAsync("/api/companies", new CreateCompanyRequest("Acme Corp", "https://www.acme.com"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Duplicate_registration_returns_409_and_weak_password_400()
    {
        var email = NewEmail();
        await Register(email);
        var dup = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email.ToUpperInvariant(), "password123"));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var weak = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(NewEmail(), "short"));
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [Fact]
    public async Task Login_succeeds_with_right_password_and_returns_401_otherwise()
    {
        var email = NewEmail();
        await Register(email);

        var ok = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "password123"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var bad = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "nope-nope"));
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        var unknown = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(NewEmail(), "password123"));
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
    }

    [Fact]
    public async Task Seeded_default_user_can_log_in()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@example.com", "Admin123!"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task List_is_paged_and_invalid_paging_returns_400()
    {
        await SignIn();
        foreach (var n in new[] { "Pagera", "Pagerb", "Pagerc" })
            Assert.Equal(HttpStatusCode.Created, (await Post(n, $"https://{n.ToLowerInvariant()}.io")).StatusCode);

        var first = (await _client.GetFromJsonAsync<PagedResponse<CompanyResponse>>("/api/companies?domain=pager&page=1&pageSize=2&sort=name"))!;
        Assert.Equal(["Pagera", "Pagerb"], first.Items.Select(c => c.Name));
        Assert.Equal(3, first.Total);
        Assert.Equal(2, first.PageSize);

        var second = (await _client.GetFromJsonAsync<PagedResponse<CompanyResponse>>("/api/companies?domain=pager&page=2&pageSize=2&sort=name"))!;
        Assert.Equal(["Pagerc"], second.Items.Select(c => c.Name));

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/companies?pageSize=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/companies?sort=nope")).StatusCode);
    }

    [Fact]
    public async Task Unknown_id_returns_404()
    {
        await SignIn();
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/companies/{Guid.NewGuid()}")).StatusCode);
    }

    [Theory]
    [InlineData("/api/companies")]
    [InlineData("/api/companies/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/companies/search?q=acme")]
    public async Task Reading_companies_without_token_returns_401(string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(url)).StatusCode);

    [Fact]
    public async Task Search_orders_by_relevance_and_empty_result_is_200()
    {
        await SignIn();
        await Post("Zed Labs", "https://zedlabs.io");
        await Post("Zed Labs Extra", "https://zedlabsextra.io");

        var results = (await _client.GetFromJsonAsync<PagedResponse<CompanySearchResult>>("/api/companies/search?q=zed labs"))!.Items;
        Assert.True(results.Count >= 2);
        Assert.Equal(results.OrderByDescending(r => r.Relevance), results);

        var empty = await _client.GetAsync("/api/companies/search?q=qqqqqqq");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Empty((await empty.Content.ReadFromJsonAsync<PagedResponse<CompanySearchResult>>())!.Items);
    }
}
