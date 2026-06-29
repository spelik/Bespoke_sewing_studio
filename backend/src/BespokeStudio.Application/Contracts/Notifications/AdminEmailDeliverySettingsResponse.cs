namespace BespokeStudio.Application.Contracts.Notifications;

public sealed record AdminEmailDeliverySettingsResponse(
    string Provider,
    string? GmailAddress,
    string SenderName,
    bool AppPasswordConfigured,
    DateTimeOffset? UpdatedAt);
