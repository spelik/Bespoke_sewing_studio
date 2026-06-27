using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class PortfolioItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public PortfolioItemStatus Status { get; set; } = PortfolioItemStatus.Draft;
    public Guid? CoverImageFileId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
