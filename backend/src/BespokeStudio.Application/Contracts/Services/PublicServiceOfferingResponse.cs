namespace BespokeStudio.Application.Contracts.Services;

public sealed record PublicServiceOfferingResponse(
    Guid Id,
    string Slug,
    string Name,
    string ShortDescription,
    string? Description,
    string? Category,
    bool IsFeatured,
    int DisplayOrder,
    IReadOnlyCollection<ServicePriceOptionResponse> PriceOptions,
    string? ImageUrl);
