namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record AdminSiteSettingsResponse(
    Guid Id,
    string StudioName,
    string? SiteTagline,
    string? Email,
    string? Phone,
    string? ContactButtonLabel,
    string? ContactIntroText,
    bool EmailNotificationsEnabled,
    bool WhatsAppNotificationsEnabled,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TikTokUrl,
    string? PinterestUrl,
    string? FooterText,
    string? ServiceAreaText,
    string? BusinessLegalName,
    DateTimeOffset UpdatedAt);
