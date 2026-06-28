namespace BespokeStudio.Domain.Entities;

public sealed class ServicePriceOption
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ServiceOfferingId { get; set; }
    public required string Label { get; set; }
    public string? Description { get; set; }
    public required string PriceText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
