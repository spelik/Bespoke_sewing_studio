using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public OrderServiceType ServiceType { get; set; }
    public Guid? ServiceOfferingId { get; set; }
    public required string ServiceNameSnapshot { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public required string Description { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTimeOffset? ConsentRecordedAt { get; set; }
    public decimal? QuotedAmount { get; set; }
    public string Currency { get; set; } = "GBP";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ServiceOffering? ServiceOffering { get; set; }
}
