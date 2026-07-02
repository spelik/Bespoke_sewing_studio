namespace BespokeStudio.Application.Contracts.Storage;

public sealed record ScheduleUploadFileDeletionRequest(
    string StorageKey,
    string? OriginalFileName,
    long? FileSizeBytes,
    string RelatedEntityType,
    Guid? RelatedEntityId,
    string Reason);
