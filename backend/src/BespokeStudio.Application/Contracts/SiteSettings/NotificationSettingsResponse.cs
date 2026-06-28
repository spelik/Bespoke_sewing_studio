namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record NotificationSettingsResponse(
    string? Email,
    bool EmailNotificationsEnabled);
