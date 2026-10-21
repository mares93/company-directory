namespace CompanyApi.Application.Dtos;

public record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    /// <summary>Takes the requested page from <paramref name="all"/> and maps only those items.</summary>
    public static PagedResponse<T> From<TSource>(IReadOnlyList<TSource> all, PageRequest p, Func<TSource, T> map)
    {
        // long: (Page - 1) * PageSize overflows int for huge pages and would wrap to a negative skip.
        var skip = (long)(p.Page - 1) * p.PageSize;
        IReadOnlyList<T> items = skip >= all.Count ? [] : all.Skip((int)skip).Take(p.PageSize).Select(map).ToList();
        return new PagedResponse<T>(items, all.Count, p.Page, p.PageSize);
    }
}
