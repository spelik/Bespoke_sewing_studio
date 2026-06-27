namespace BespokeStudio.Application.Contracts.Uploads;

public sealed record UploadCleanupResponse(
    int ScannedCount,
    int DeletedMetadataCount,
    int DeletedPhysicalFilesCount,
    int MissingFilesCount,
    int SkippedCount);
