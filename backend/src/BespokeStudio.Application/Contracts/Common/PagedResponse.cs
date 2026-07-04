namespace BespokeStudio.Application.Contracts.Common;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static PagedResponse<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        return new PagedResponse<T>(items, page, pageSize, totalItems, totalPages);
    }
}
