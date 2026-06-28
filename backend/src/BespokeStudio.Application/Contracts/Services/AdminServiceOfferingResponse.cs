namespace BespokeStudio.Application.Contracts.Services;

public sealed record AdminServiceOfferingResponse(
    Guid Id,
    string Slug,
    string Name,
    string ShortDescription,
    string? Description,
    string? Category,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    IReadOnlyCollection<ServicePriceOptionResponse> PriceOptions,
    string? ImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt,
    int UsageCount,
    bool CanDelete);
