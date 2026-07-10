namespace BespokeStudio.Application.Contracts.Notifications;

public sealed record AdminEmailDeliverySettingsResponse(
    string Provider,
    string? GmailAddress,
    string SenderName,
    bool AppPasswordConfigured,
    string? ResendFromEmail,
    string? ReplyToEmail,
    bool ResendApiKeyConfigured,
    DateTimeOffset? UpdatedAt);
