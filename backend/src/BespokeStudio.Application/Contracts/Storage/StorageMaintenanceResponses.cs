namespace BespokeStudio.Application.Contracts.Storage;

public sealed record StorageScanResponse(
    int DatabaseFileCount,
    int PhysicalFileCount,
    long TotalPhysicalBytes,
    int OrphanPhysicalFileCount,
    long OrphanPhysicalBytes,
    int MissingPhysicalFileCount,
    DateTimeOffset ScannedAt,
    IReadOnlyList<OrphanPhysicalFileResponse> OrphanPhysicalFiles,
    IReadOnlyList<MissingPhysicalFileResponse> MissingPhysicalFiles,
    StorageCleanupJobSummaryResponse CleanupJobs);

public sealed record StorageCleanupJobSummaryResponse(
    int PendingCount,
    int ProcessingCount,
    int FailedCount,
    int SucceededCount,
    int SkippedCount,
    IReadOnlyList<FailedStorageCleanupJobResponse> FailedJobs);

public sealed record FailedStorageCleanupJobResponse(
    Guid Id,
    string StorageKey,
    string Reason,
    int Attempts,
    int MaxAttempts,
    string? LastError,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrphanPhysicalFileResponse(
    string RelativePath,
    long SizeBytes,
    DateTimeOffset? LastModifiedAt);

public sealed record MissingPhysicalFileResponse(
    Guid UploadedFileId,
    string OriginalFileName,
    string StorageKey,
    string Purpose,
    string? RelatedInfo);

public sealed record StorageCleanupResponse(
    int DeletedCount,
    long DeletedBytes,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<StorageCleanupFailureResponse> FailedItems);

public sealed record StorageCleanupFailureResponse(
    string RelativePath,
    string Reason);
