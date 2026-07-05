namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailOutboxMonitoringSummaryResponse(
    int PendingCount,
    int ProcessingCount,
    int RetryingCount,
    int FailedCount,
    int ExhaustedFailedCount,
    int StalePendingCount,
    int SentLast24HoursCount,
    int FailedLast24HoursCount,
    DateTimeOffset? OldestPendingCreatedAt,
    DateTimeOffset? OldestFailedUpdatedAt,
    DateTimeOffset GeneratedAt,
    int StalePendingThresholdMinutes,
    string HealthStatus,
    string SummaryMessage);
