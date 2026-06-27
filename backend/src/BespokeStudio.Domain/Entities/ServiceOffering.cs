using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class ServiceOffering
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public OrderServiceType ServiceType { get; set; }
    public required string Name { get; set; }
    public required string ShortDescription { get; set; }
    public string? DetailedDescription { get; set; }
    public decimal? StartingPrice { get; set; }
    public string Currency { get; set; } = "GBP";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
