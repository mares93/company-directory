namespace CompanyApi.Application.Services;

public interface IContentRelevanceChecker
{
    /// <summary>Similarity in [0,1] between the company name and the page's own content; 0 when it cannot be read.</summary>
    Task<double> ScoreAsync(string name, Uri url, CancellationToken ct = default);
}
