namespace BespokeStudio.Application.Contracts.InStock;

public sealed record AdminInStockImageResponse(
    Guid Id,
    Guid UploadedFileId,
    string ImageUrl,
    string? AltText,
    int DisplayOrder,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAt);
