using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class InStockItem
{
    public const string DefaultCurrency = "GBP";

    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = DefaultCurrency;
    public InStockItemStatus Status { get; set; } = InStockItemStatus.Available;
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }
    public string? Sizes { get; set; }
    public string? Materials { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ArchivedAt { get; set; }
    public ICollection<InStockItemImage> Images { get; } = new List<InStockItemImage>();
}
