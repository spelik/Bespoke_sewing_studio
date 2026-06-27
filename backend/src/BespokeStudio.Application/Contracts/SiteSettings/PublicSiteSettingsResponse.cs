namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record PublicSiteSettingsResponse(
    string StudioName,
    string? SiteTagline,
    string? PublicEmail,
    string? PublicPhone,
    string? WhatsAppPhone,
    string? ContactButtonLabel,
    string? ContactIntroText,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TikTokUrl,
    string? PinterestUrl,
    string? FooterText,
    string? ServiceAreaText);
