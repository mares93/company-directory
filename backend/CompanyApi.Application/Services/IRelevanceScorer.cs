namespace CompanyApi.Application.Services;

public interface IRelevanceScorer
{
    /// <summary>Similarity in [0,1] between a free text (company name or query) and a domain.</summary>
    double Score(string text, string domain);
}
