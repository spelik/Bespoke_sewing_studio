namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailOutboxRetentionCleanupResponse(
    int SucceededBodyPurgedCount,
    int SkippedBodyPurgedCount,
    int SucceededDeletedCount,
    int SkippedDeletedCount,
    DateTimeOffset CompletedAt,
    string ResultMessage);
