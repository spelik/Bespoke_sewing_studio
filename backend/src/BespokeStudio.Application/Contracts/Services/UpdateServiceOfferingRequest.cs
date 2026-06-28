namespace BespokeStudio.Application.Contracts.Services;

public sealed record UpdateServiceOfferingRequest(
    string? Slug,
    string Name,
    string ShortDescription,
    string? Description,
    string? Category,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    IReadOnlyCollection<ServicePriceOptionRequest>? PriceOptions,
    string? ImageUrl);
