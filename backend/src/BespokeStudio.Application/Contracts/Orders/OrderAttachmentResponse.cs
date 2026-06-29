using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Orders;

public sealed record OrderAttachmentResponse(
    Guid Id,
    Guid UploadedFileId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string? Caption,
    int DisplayOrder,
    UploadScanStatus ScanStatus,
    string? ScanProvider,
    DateTimeOffset? ScannedAt);
