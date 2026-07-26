using BespokeStudio.Api.Caching;
using Microsoft.AspNetCore.OutputCaching;

namespace BespokeStudio.Tests.Caching;

public sealed class PublicOutputCachePolicyTests
{
    [Fact]
    public void PublicContentPolicy_UsesShortPublicCacheLifetime()
    {
        Assert.Equal("PublicContent", PublicOutputCachePolicy.Name);
        Assert.Equal(60, PublicOutputCachePolicy.DurationSeconds);
        Assert.Equal(TimeSpan.FromSeconds(60), PublicOutputCachePolicy.Duration);
    }

    [Theory]
    [InlineData(nameof(PublicOutputCachePolicy.AllPublicContentTag), "public-content")]
    [InlineData(nameof(PublicOutputCachePolicy.ServicesTag), "public-services")]
    [InlineData(nameof(PublicOutputCachePolicy.PortfolioTag), "public-portfolio")]
    [InlineData(nameof(PublicOutputCachePolicy.InStockTag), "public-in-stock")]
    [InlineData(nameof(PublicOutputCachePolicy.PageContentTag), "public-page-content")]
    [InlineData(nameof(PublicOutputCachePolicy.RepeatableContentTag), "public-repeatable-content")]
    [InlineData(nameof(PublicOutputCachePolicy.SiteSettingsTag), "public-site-settings")]
    [InlineData(nameof(PublicOutputCachePolicy.BrandSettingsTag), "public-brand-settings")]
    public void TagConstants_UseExpectedValues(string propertyName, string expectedValue)
    {
        var actualValue = typeof(PublicOutputCachePolicy).GetField(propertyName)!.GetValue(null);
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public async Task EvictAsync_EvictsEachDistinctNonEmptyTagOnce()
    {
        var store = new FakeOutputCacheStore();

        await PublicOutputCacheInvalidation.EvictAsync(
            store,
            CancellationToken.None,
            PublicOutputCachePolicy.ServicesTag,
            PublicOutputCachePolicy.ServicesTag,
            " ",
            PublicOutputCachePolicy.PortfolioTag);

        Assert.Equal(
            [PublicOutputCachePolicy.ServicesTag, PublicOutputCachePolicy.PortfolioTag],
            store.EvictedTags);
    }

    [Fact]
    public async Task EvictAsync_IsNoOpWhenTagsAreEmpty()
    {
        var store = new FakeOutputCacheStore();

        await PublicOutputCacheInvalidation.EvictAsync(store, CancellationToken.None);
        await PublicOutputCacheInvalidation.EvictAsync(store, CancellationToken.None, null!);
        await PublicOutputCacheInvalidation.EvictAsync(store, CancellationToken.None, "", "   ");

        Assert.Empty(store.EvictedTags);
    }

    private sealed class FakeOutputCacheStore : IOutputCacheStore
    {
        public List<string> EvictedTags { get; } = [];

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken) =>
            ValueTask.FromResult<byte[]?>(null);

        public ValueTask SetAsync(
            string key,
            byte[] value,
            string[]? tags,
            TimeSpan validFor,
            CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            EvictedTags.Add(tag);
            return ValueTask.CompletedTask;
        }
    }
}
