namespace BespokeStudio.Domain.Entities;

public sealed class InStockItemImage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid InStockItemId { get; set; }
    public Guid UploadedFileId { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public InStockItem? Item { get; set; }
    public UploadedFileMetadata? UploadedFile { get; set; }
}
