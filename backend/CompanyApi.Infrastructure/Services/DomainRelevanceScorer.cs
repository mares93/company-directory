using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CompanyApi.Application.Services;

namespace CompanyApi.Infrastructure.Services;

/// <summary>
/// Explainable, offline strategy: compare the normalized text with the domain's main label
/// (e.g. "www.acme-tools.com" -> "acmetools", "cemex.com.mx" -> "cemex"). Exact match = 1,
/// containment = 0.8, otherwise 0.8 x the share of the text's words found at the start or end of the label.
/// Words shorter than 3 letters never count on their own: they match almost any label.
/// shortcut: the content of the page is a separate scorer (HttpContentRelevanceChecker), off by default.
/// </summary>
public partial class DomainRelevanceScorer : IRelevanceScorer
{
    internal const int MinWordLength = 3;

    private static readonly HashSet<string> Noise = ["inc", "llc", "ltd", "corp", "corporation", "co", "company", "group", "sa", "the", "and"];

    // Second-level labels of country domains (acme.co.uk, cemex.com.mx): the brand is the label before them.
    private static readonly HashSet<string> GenericSecondLevel = ["co", "com", "org", "net", "gov", "edu", "ac"];

    public double Score(string text, string domain)
    {
        var words = Words(text);
        var compact = string.Concat(words);
        var label = Normalize(MainLabel(domain)).Replace(" ", "");
        if (compact.Length == 0 || label.Length == 0) return 0;

        if (compact == label) return 1;
        if ((label.Length >= MinWordLength && compact.Contains(label, StringComparison.Ordinal))
            || (compact.Length >= MinWordLength
                && (label.StartsWith(compact, StringComparison.Ordinal) || label.EndsWith(compact, StringComparison.Ordinal))))
            return 0.8;

        var matched = words.Count(w => w.Length >= MinWordLength
            && (label.StartsWith(w, StringComparison.Ordinal) || label.EndsWith(w, StringComparison.Ordinal)));
        return 0.8 * matched / words.Count;
    }

    private static string MainLabel(string domain)
    {
        var parts = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && GenericSecondLevel.Contains(parts[^2])) return parts[^3];
        return parts.Length >= 2 ? parts[^2] : domain;
    }

    internal static List<string> Words(string text) =>
        Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !Noise.Contains(w)).ToList();

    // Lowercase, strip accents and punctuation.
    internal static string Normalize(string s)
    {
        var d = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return NonAlnum().Replace(sb.ToString(), " ").Trim();
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlnum();
}
