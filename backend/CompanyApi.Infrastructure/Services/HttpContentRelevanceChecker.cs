using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using CompanyApi.Application.Services;

namespace CompanyApi.Infrastructure.Services;

/// <summary>
/// Downloads the page and looks for the company name words in its title, description and og:site_name.
/// The URL is user supplied, so connections to private/loopback addresses are blocked (SSRF),
/// with a short timeout and a size cap. Any failure scores 0 (the domain rule still applies).
/// </summary>
public partial class HttpContentRelevanceChecker(HttpClient http) : IContentRelevanceChecker
{
    private const int MaxBytes = 256 * 1024;
    // The page is attacker-controlled: a crafted one can make these patterns backtrack for minutes.
    private const int MatchTimeoutMs = 200;

    public async Task<double> ScoreAsync(string name, Uri url, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!res.IsSuccessStatusCode || res.Content.Headers.ContentType?.MediaType != "text/html") return 0;

            await using var stream = await res.Content.ReadAsStreamAsync(cts.Token);
            var buffer = new byte[MaxBytes];
            var read = 0;
            int n;
            while (read < MaxBytes && (n = await stream.ReadAsync(buffer.AsMemory(read), cts.Token)) > 0) read += n;
            return ScoreHtml(name, Encoding.UTF8.GetString(buffer, 0, read));
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return 0;
        }
    }

    /// <summary>Share of the name's words found in the page's title, description and site name.</summary>
    public static double ScoreHtml(string name, string html)
    {
        var words = DomainRelevanceScorer.Words(name).Where(w => w.Length >= DomainRelevanceScorer.MinWordLength).ToList();
        if (words.Count == 0) return 0;

        try
        {
            var text = string.Join(" ", new[] { Title().Match(html), MetaDescription().Match(html), SiteName().Match(html) }
                .Where(m => m.Success).Select(m => m.Groups[1].Value));
            var haystack = DomainRelevanceScorer.Normalize(WebUtility.HtmlDecode(text)).Replace(" ", "");
            return (double)words.Count(haystack.Contains) / words.Count;
        }
        catch (RegexMatchTimeoutException)
        {
            return 0;
        }
    }

    public static bool IsPublicAddress(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return false;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || (ip.GetAddressBytes()[0] & 0xFE) == 0xFC);

        var b = ip.GetAddressBytes();
        return !(b[0] is 10 or 0 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254));
    }

    /// <summary>Connects only to public addresses; runs on every connection, so redirects are covered too.</summary>
    public static async ValueTask<Stream> ConnectToPublicAddress(SocketsHttpConnectionContext ctx, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, ct);
        var ip = addresses.FirstOrDefault(IsPublicAddress) ?? throw new HttpRequestException("Blocked non-public address.");
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(ip, ctx.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    [GeneratedRegex("<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline, MatchTimeoutMs)]
    private static partial Regex Title();

    [GeneratedRegex("<meta[^>]+name=[\"']description[\"'][^>]+content=[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase, MatchTimeoutMs)]
    private static partial Regex MetaDescription();

    [GeneratedRegex("<meta[^>]+property=[\"']og:site_name[\"'][^>]+content=[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase, MatchTimeoutMs)]
    private static partial Regex SiteName();
}
