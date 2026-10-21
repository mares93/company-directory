using System.Diagnostics;
using System.Net;
using System.Text;
using CompanyApi.Infrastructure.Services;

namespace CompanyApi.Tests;

public class ContentRelevanceTests
{
    [Fact]
    public void ScoreHtml_finds_name_in_title_description_or_site_name()
    {
        const string html = "<html><head><title>Alphabet Inc. &amp; friends</title><meta name=\"description\" content=\"Holding company\"></head></html>";
        Assert.Equal(1, HttpContentRelevanceChecker.ScoreHtml("Alphabet Inc", html));
        Assert.Equal(0, HttpContentRelevanceChecker.ScoreHtml("Acme", html));
    }

    [Fact]
    public void ScoreHtml_gives_up_quickly_on_a_page_built_to_make_the_regex_backtrack()
    {
        var hostile = string.Concat(Enumerable.Repeat("<meta name=\"description\" ", 10_000)); // ~260 KB, no content=
        var watch = Stopwatch.StartNew();
        var score = HttpContentRelevanceChecker.ScoreHtml("Alphabet Inc", hostile);
        Assert.Equal(0, score);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"took {watch.Elapsed}");
    }

    [Fact]
    public async Task Checker_never_connects_to_a_loopback_server()
    {
        var port = Random.Shared.Next(20000, 40000);
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        var requests = 0;
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var ctx = await listener.GetContextAsync();
                    Interlocked.Increment(ref requests);
                    var body = Encoding.UTF8.GetBytes("<html><head><title>Alphabet</title></head></html>");
                    ctx.Response.ContentType = "text/html";
                    await ctx.Response.OutputStream.WriteAsync(body);
                    ctx.Response.Close();
                }
            }
            catch (Exception) { /* listener stopped */ }
        });
        var url = new Uri($"http://127.0.0.1:{port}/");

        // Control: without the guard the page is reachable, so the test is meaningful.
        using var open = new HttpClient();
        Assert.Equal(1, await new HttpContentRelevanceChecker(open).ScoreAsync("Alphabet", url));
        Assert.Equal(1, requests);

        using var guarded = new HttpClient(new SocketsHttpHandler { ConnectCallback = HttpContentRelevanceChecker.ConnectToPublicAddress });
        Assert.Equal(0, await new HttpContentRelevanceChecker(guarded).ScoreAsync("Alphabet", url));
        Assert.Equal(1, requests); // the guarded client never reached the server
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.0.0.5", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("::1", false)]
    [InlineData("fd00::1", false)]
    public void IsPublicAddress_blocks_private_ranges(string ip, bool expected) =>
        Assert.Equal(expected, HttpContentRelevanceChecker.IsPublicAddress(System.Net.IPAddress.Parse(ip)));
}
