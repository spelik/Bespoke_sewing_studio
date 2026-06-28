namespace BespokeStudio.Domain.Entities;

public sealed class PageContent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string PageKey { get; set; }
    public required string SectionKey { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Body { get; set; }
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }
    public Guid? ImageFileId { get; set; }
    public string? ImageAltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ArchivedAt { get; set; }
    public UploadedFileMetadata? ImageFile { get; set; }
}
