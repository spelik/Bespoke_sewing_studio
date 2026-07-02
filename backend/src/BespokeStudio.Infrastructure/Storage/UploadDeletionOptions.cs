namespace BespokeStudio.Infrastructure.Storage;

public sealed class UploadDeletionOptions
{
    public const string SectionName = "UploadDeletion";

    public int PollIntervalSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 20;
    public int MaxAttempts { get; init; } = 5;
    public int BaseRetrySeconds { get; init; } = 30;
    public int ProcessingTimeoutMinutes { get; init; } = 5;
}
