namespace BespokeStudio.Domain.Entities;

public sealed class PortfolioItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string? Slug { get; set; }
    public required string Title { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public Guid? CoverImageFileId { get; set; }
    public required string AltText { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ArchivedAt { get; set; }
    public PortfolioCategory? Category { get; set; }
    public UploadedFileMetadata? CoverImageFile { get; set; }
}
