using BespokeStudio.Api.Caching;

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
}
