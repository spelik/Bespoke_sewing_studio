using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace BespokeStudio.Tests.InStock;

public sealed class SitemapServiceTests
{
    [Fact]
    public async Task BuildXml_IncludesCatalogueAndPublishedItemUrls_WithEscaping()
    {
        var inStock = new StubInStockService(
        [
            CreateItem("silk-blouse"),
            CreateItem("coat-&-hat"),
        ]);
        var service = new SitemapService(inStock, new ConfigurationBuilder().Build());

        var xml = await service.BuildXmlAsync();

        Assert.Contains("https://oksanalogosha.com/in-stock</loc>", xml);
        Assert.Contains("https://oksanalogosha.com/in-stock/silk-blouse</loc>", xml);
        Assert.Contains("https://oksanalogosha.com/in-stock/coat-&amp;-hat</loc>", xml);
        Assert.Contains("https://oksanalogosha.com/services</loc>", xml);
        Assert.Contains("https://oksanalogosha.com/portfolio</loc>", xml);
        Assert.DoesNotContain("/admin", xml);
        Assert.DoesNotContain("draft-item", xml);
    }

    [Fact]
    public async Task BuildXml_UsesConfiguredPublicSiteUrl()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicSiteUrl"] = "https://example.test/",
            })
            .Build();
        var service = new SitemapService(new StubInStockService([]), config);

        var xml = await service.BuildXmlAsync();

        Assert.Contains("https://example.test/in-stock</loc>", xml);
        Assert.DoesNotContain("https://example.test//", xml);
    }

    private static PublicInStockItemResponse CreateItem(string slug) =>
        new(
            Guid.NewGuid(),
            slug,
            slug,
            null,
            null,
            10m,
            "GBP",
            InStockItemStatus.Available,
            null,
            null,
            Array.Empty<PublicInStockImageResponse>());

    private sealed class StubInStockService(IReadOnlyList<PublicInStockItemResponse> items) : IInStockService
    {
        public Task<IReadOnlyList<PublicInStockItemResponse>> GetPublicItemsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items);

        public Task<PublicInStockItemResponse?> GetPublicItemBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AdminInStockItemResponse>> GetAdminItemsAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockItemResponse?> GetAdminItemByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockItemResponse> CreateItemAsync(
            SaveInStockItemRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockItemResponse?> UpdateItemAsync(
            Guid id,
            SaveInStockItemRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ArchiveInStockItemResponse?> ArchiveItemAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ArchiveInStockItemResponse?> RestoreItemAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockImageResponse?> AddImageAsync(
            Guid itemId,
            BespokeStudio.Application.Contracts.Uploads.UploadFileRequest file,
            string? altText,
            int? displayOrder,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockImageResponse?> UpdateImageAsync(
            Guid itemId,
            Guid imageId,
            UpdateInStockImageRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AdminInStockImageResponse>?> ReorderImagesAsync(
            Guid itemId,
            ReorderInStockImagesRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteImageAsync(
            Guid itemId,
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
