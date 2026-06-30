namespace BespokeStudio.Application.Contracts.Orders;

public sealed record DeleteOrderAttachmentResult(
    Guid OrderId,
    string? OrderReferenceNumber,
    Guid AttachmentId,
    Guid UploadedFileId,
    string OriginalFileName,
    bool PhysicalFileDeleted);
