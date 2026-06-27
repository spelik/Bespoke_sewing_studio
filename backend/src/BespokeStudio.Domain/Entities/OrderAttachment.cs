namespace BespokeStudio.Domain.Entities;

public sealed class OrderAttachment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid UploadedFileId { get; set; }
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
