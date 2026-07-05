namespace BespokeStudio.Infrastructure.Notifications;

public sealed class EmailOutboxRetentionOptions
{
    public const string SectionName = "EmailOutboxRetention";

    public bool WorkerEnabled { get; init; }
    public int WorkerIntervalHours { get; init; } = 24;
    public int BatchSize { get; init; } = 200;
    public int SucceededBodyRetentionDays { get; init; } = 30;
    public int SucceededMessageRetentionDays { get; init; } = 90;
    public int SkippedBodyRetentionDays { get; init; } = 30;
    public int SkippedMessageRetentionDays { get; init; } = 90;
    public string PurgedBodyPlaceholder { get; init; } =
        "[Email body purged by retention policy.]";
}
