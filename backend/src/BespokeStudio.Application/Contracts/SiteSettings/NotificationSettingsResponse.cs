namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record NotificationSettingsResponse(
    string? Email,
    string? Phone,
    bool EmailNotificationsEnabled,
    bool WhatsAppNotificationsEnabled);
