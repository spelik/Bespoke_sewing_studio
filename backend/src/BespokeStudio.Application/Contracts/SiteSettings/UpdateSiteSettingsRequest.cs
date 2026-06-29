namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record UpdateSiteSettingsRequest(
    string StudioName,
    string? SiteTagline,
    string? Email,
    string? Phone,
    string? ContactButtonLabel,
    string? ContactIntroText,
    bool EmailNotificationsEnabled,
    bool CustomerConfirmationEmailsEnabled,
    string CustomerOrderConfirmationSubject,
    string CustomerOrderConfirmationBody,
    string CustomerContactConfirmationSubject,
    string CustomerContactConfirmationBody,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TikTokUrl,
    string? PinterestUrl,
    string? FooterText,
    string? ServiceAreaText,
    string? BusinessLegalName);
