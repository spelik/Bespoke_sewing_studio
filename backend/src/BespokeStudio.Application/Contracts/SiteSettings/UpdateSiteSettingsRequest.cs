namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record UpdateSiteSettingsRequest(
    string StudioName,
    string? SiteTagline,
    string? PublicEmail,
    string? PublicPhone,
    string? WhatsAppPhone,
    string? ContactButtonLabel,
    string? ContactIntroText,
    string? NotificationEmail,
    string? NotificationPhone,
    bool EmailNotificationsEnabled,
    bool WhatsAppNotificationsEnabled,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TikTokUrl,
    string? PinterestUrl,
    string? FooterText,
    string? ServiceAreaText,
    string? BusinessLegalName);
