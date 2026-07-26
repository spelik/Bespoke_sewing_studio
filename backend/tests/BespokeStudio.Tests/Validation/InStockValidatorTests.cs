using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Tests.Validation;

public sealed class InStockValidatorTests
{
    [Fact]
    public void Validate_RejectsNegativePriceAndInvalidCurrency()
    {
        var errors = InStockValidator.Validate(CreateRequest(price: -1m, currency: "USD"));

        Assert.Contains("Price", errors.Keys);
        Assert.Contains("Currency", errors.Keys);
    }

    [Fact]
    public void Validate_AcceptsGbpDefaultAndZeroPrice()
    {
        var errors = InStockValidator.Validate(CreateRequest(price: 0m, currency: null));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RejectsInvalidSlugAndMissingTitle()
    {
        var errors = InStockValidator.Validate(CreateRequest(title: " ", slug: "Bad Slug"));

        Assert.Contains("Title", errors.Keys);
        Assert.Contains("Slug", errors.Keys);
    }

    [Fact]
    public void ValidateImage_RejectsNegativeDisplayOrder()
    {
        var errors = InStockValidator.Validate(new UpdateInStockImageRequest("alt", -1));

        Assert.Contains("DisplayOrder", errors.Keys);
    }

    [Theory]
    [InlineData(null, true, null)]
    [InlineData("", true, null)]
    [InlineData("  ", true, null)]
    [InlineData("3", true, 3)]
    [InlineData("not-a-number", false, null)]
    [InlineData("-1", false, null)]
    public void TryParseOptionalDisplayOrder_ValidatesMultipartValues(
        string? raw,
        bool expectedOk,
        int? expectedValue)
    {
        var ok = InStockValidator.TryParseOptionalDisplayOrder(raw, out var value, out var error);

        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedValue, value);
        if (!expectedOk)
        {
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }

    private static SaveInStockItemRequest CreateRequest(
        string title = "Linen dress",
        string? slug = "linen-dress",
        decimal price = 120m,
        string? currency = "GBP") =>
        new(
            slug,
            title,
            "Short",
            "Long description",
            price,
            currency,
            InStockItemStatus.Available,
            true,
            0,
            "M",
            "Linen");
}
