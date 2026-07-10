namespace BespokeStudio.Application.Contracts.Notifications;

public sealed record UpdateEmailDeliverySettingsRequest(
    string Provider,
    string? GmailAddress,
    string? SenderName,
    string? AppPassword,
    bool ClearAppPassword,
    string? ResendFromEmail,
    string? ReplyToEmail,
    string? ResendApiKey,
    bool ClearResendApiKey);
