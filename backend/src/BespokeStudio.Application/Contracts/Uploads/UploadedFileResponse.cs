using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Uploads;

public sealed record UploadedFileResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    UploadPurpose Purpose,
    DateTimeOffset CreatedAt);
