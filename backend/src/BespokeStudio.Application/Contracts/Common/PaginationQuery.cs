namespace BespokeStudio.Application.Contracts.Common;

public static class PaginationQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    private static readonly int[] AllowedPageSizes = [10, 25, 50, 100];

    public static PaginationParameters Normalize(int? page, int? pageSize)
    {
        var normalizedPage = page.GetValueOrDefault(DefaultPage);
        if (normalizedPage < 1)
        {
            normalizedPage = DefaultPage;
        }

        var requestedPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
        var normalizedPageSize = requestedPageSize > MaxPageSize
            ? MaxPageSize
            : AllowedPageSizes.Contains(requestedPageSize)
                ? requestedPageSize
                : DefaultPageSize;

        return new PaginationParameters(normalizedPage, normalizedPageSize);
    }
}

public sealed record PaginationParameters(int Page, int PageSize)
{
    public int Skip => (int)Math.Min((long)(Page - 1) * PageSize, int.MaxValue);
}
