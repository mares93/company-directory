using CompanyApi.Infrastructure.Services;

namespace CompanyApi.Tests;

public class RelevanceScorerTests
{
    private readonly DomainRelevanceScorer _scorer = new();

    [Theory]
    [InlineData("Acme Corp", "acme.com", true)]
    [InlineData("Acme Tools", "www.acme-tools.com", true)]
    [InlineData("Café Müller", "cafemuller.de", true)]
    [InlineData("Acme", "google.com", false)]
    [InlineData("Cemex", "cemex.com.mx", true)]   // country domains: the brand is not the second-to-last label
    [InlineData("Acme", "acme.co.uk", true)]
    [InlineData("3M Company", "3m.com", true)]
    [InlineData("Tea", "tealeaf.com", true)]
    [InlineData("Q A Z", "zebra.com", false)]     // one-letter words match almost any label
    [InlineData("Tea", "steakhouse.com", false)]  // a word only counts at the start or end of the label
    [InlineData("Banana", "a.com", false)]
    public void Score_matches_name_to_domain(string name, string domain, bool relevant) =>
        Assert.Equal(relevant, _scorer.Score(name, domain) >= 0.5);
}
