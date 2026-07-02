using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class UploadFileDeletionJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string StorageKey { get; set; }
    public string? OriginalFileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public required string RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public required string Reason { get; set; }
    public UploadFileDeletionJobStatus Status { get; set; } = UploadFileDeletionJobStatus.Pending;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? SucceededAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
