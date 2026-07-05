namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailOutboxRetentionSummaryResponse(
    bool WorkerEnabled,
    int WorkerIntervalHours,
    int BatchSize,
    int SucceededBodyRetentionDays,
    int SucceededMessageRetentionDays,
    int SkippedBodyRetentionDays,
    int SkippedMessageRetentionDays,
    int SucceededBodyPurgeCandidateCount,
    int SkippedBodyPurgeCandidateCount,
    int SucceededDeleteCandidateCount,
    int SkippedDeleteCandidateCount,
    int FailedRetainedCount,
    DateTimeOffset? OldestSucceededSentAt,
    DateTimeOffset GeneratedAt,
    string SummaryMessage);
