using CompanyApi.Application.Services;

namespace CompanyApi.Infrastructure.Services;

/// <summary>Used when content inspection is off (default): never contributes to relevance.</summary>
public class NullContentRelevanceChecker : IContentRelevanceChecker
{
    public Task<double> ScoreAsync(string name, Uri url, CancellationToken ct = default) => Task.FromResult(0d);
}
