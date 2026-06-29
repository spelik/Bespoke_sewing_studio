namespace BespokeStudio.Application.Contracts.Notifications;

public sealed record ResolvedEmailDeliverySettings(
    string Provider,
    string? GmailAddress,
    string SenderName,
    string? AppPassword,
    bool AppPasswordConfigured,
    string? ConfigurationError);
