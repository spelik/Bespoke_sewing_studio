using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Uploads;

public sealed record UploadMetadataResponse(
    Guid Id,
    UploadPurpose Purpose,
    string OriginalFileName,
    string StoredFileName,
    string StorageKey,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
