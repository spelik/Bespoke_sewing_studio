using Microsoft.AspNetCore.OutputCaching;

namespace BespokeStudio.Api.Caching;

public static class PublicOutputCachePolicy
{
    public const string Name = "PublicContent";
    public const int DurationSeconds = 60;

    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(DurationSeconds);

    public static RouteHandlerBuilder CachePublicContent(this RouteHandlerBuilder builder) =>
        builder.CacheOutput(Name);
}
