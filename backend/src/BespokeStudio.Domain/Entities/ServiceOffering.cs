namespace BespokeStudio.Domain.Entities;

public sealed class ServiceOffering
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public required string ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ArchivedAt { get; set; }
    public ICollection<ServicePriceOption> PriceOptions { get; } = new List<ServicePriceOption>();
}
