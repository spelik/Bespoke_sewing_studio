namespace BespokeStudio.Application.Contracts.Notifications;

public sealed record EmailNotificationResult(
    bool Success,
    string Provider,
    bool SentExternally,
    string Message);
