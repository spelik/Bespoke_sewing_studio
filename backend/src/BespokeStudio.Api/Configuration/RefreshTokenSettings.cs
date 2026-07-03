namespace BespokeStudio.Api.Configuration;

public sealed class RefreshTokenSettings
{
    public const string SectionName = "RefreshToken";
    public string CookieName { get; init; } = "bespoke_admin_refresh";
    public int LifetimeDays { get; init; } = 14;
}
