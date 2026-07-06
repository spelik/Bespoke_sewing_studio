using Microsoft.AspNetCore.OutputCaching;

namespace BespokeStudio.Api.Caching;

public static class PublicOutputCacheInvalidation
{
    public static async ValueTask EvictAsync(
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken,
        params string[] tags)
    {
        if (tags is null || tags.Length == 0)
        {
            return;
        }

        foreach (var tag in tags
                     .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                     .Distinct(StringComparer.Ordinal))
        {
            await outputCacheStore.EvictByTagAsync(tag, cancellationToken);
        }
    }
}
