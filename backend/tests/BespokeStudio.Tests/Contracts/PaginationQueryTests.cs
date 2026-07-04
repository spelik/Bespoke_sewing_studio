using BespokeStudio.Application.Contracts.Common;

namespace BespokeStudio.Tests.Contracts;

public sealed class PaginationQueryTests
{
    [Fact]
    public void Normalize_MissingValues_ReturnsDefaults()
    {
        var result = PaginationQuery.Normalize(null, null);

        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(0, result.Skip);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Normalize_InvalidPage_ReturnsFirstPage(int page)
    {
        var result = PaginationQuery.Normalize(page, 50);

        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(75, 25)]
    [InlineData(101, 100)]
    [InlineData(500, 100)]
    public void Normalize_InvalidPageSize_ReturnsSafePageSize(
        int pageSize,
        int expectedPageSize)
    {
        var result = PaginationQuery.Normalize(2, pageSize);

        Assert.Equal(expectedPageSize, result.PageSize);
    }
}
