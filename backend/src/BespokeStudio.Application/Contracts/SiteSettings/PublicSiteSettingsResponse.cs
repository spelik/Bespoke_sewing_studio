namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record PublicSiteSettingsResponse(
    string StudioName,
    string? SiteTagline,
    string? Email,
    string? Phone,
    string? ContactButtonLabel,
    string? ContactIntroText,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TikTokUrl,
    string? PinterestUrl,
    string? FooterText,
    string? ServiceAreaText);
