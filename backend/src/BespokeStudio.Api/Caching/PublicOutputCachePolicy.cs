using Microsoft.AspNetCore.OutputCaching;

namespace BespokeStudio.Api.Caching;

public static class PublicOutputCachePolicy
{
    public const string Name = "PublicContent";
    public const int DurationSeconds = 60;

    public const string AllPublicContentTag = "public-content";
    public const string ServicesTag = "public-services";
    public const string PortfolioTag = "public-portfolio";
    public const string PageContentTag = "public-page-content";
    public const string RepeatableContentTag = "public-repeatable-content";
    public const string SiteSettingsTag = "public-site-settings";
    public const string BrandSettingsTag = "public-brand-settings";

    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(DurationSeconds);

    public static RouteHandlerBuilder CachePublicContent(
        this RouteHandlerBuilder builder,
        params string[] tags) =>
        builder.CacheOutput(policy =>
        {
            policy.Expire(Duration);
            policy.Tag(AllPublicContentTag);

            foreach (var tag in tags
                         .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                         .Distinct(StringComparer.Ordinal))
            {
                policy.Tag(tag);
            }
        });
}
