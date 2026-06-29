namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record NotificationSettingsResponse(
    string StudioName,
    string? Email,
    bool EmailNotificationsEnabled,
    bool CustomerConfirmationEmailsEnabled,
    string CustomerOrderConfirmationSubject,
    string CustomerOrderConfirmationBody,
    string CustomerContactConfirmationSubject,
    string CustomerContactConfirmationBody);
