namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record UpdateSiteSettingsRequest(
    string StudioName,
    string? SiteTagline,
    string? Email,
    string? Phone,
    string? ContactButtonLabel,
    string? ContactIntroText,
    bool EmailNotificationsEnabled,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TikTokUrl,
    string? PinterestUrl,
    string? FooterText,
    string? ServiceAreaText,
    string? BusinessLegalName);
