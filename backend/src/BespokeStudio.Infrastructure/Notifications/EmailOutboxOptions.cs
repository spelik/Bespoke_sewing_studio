namespace BespokeStudio.Infrastructure.Notifications;

public sealed class EmailOutboxOptions
{
    public const string SectionName = "EmailOutbox";

    public int WorkerIntervalSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 20;
    public int MaxAttempts { get; init; } = 5;
    public int RetryBaseSeconds { get; init; } = 60;
    public int RetryMaxMinutes { get; init; } = 60;
    public int ProcessingTimeoutMinutes { get; init; } = 5;
}
