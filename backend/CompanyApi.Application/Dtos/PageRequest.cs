namespace CompanyApi.Application.Dtos;

/// <summary>Paging and ordering query. Sort is "relevance" (default), "name" or "newest".</summary>
public record PageRequest(int Page = 1, int PageSize = 10, string? Sort = null);
