using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.InStock;

public sealed record PublicInStockItemResponse(
    Guid Id,
    string Slug,
    string Title,
    string? ShortDescription,
    string? Description,
    decimal Price,
    string Currency,
    InStockItemStatus Status,
    string? Sizes,
    string? Materials,
    IReadOnlyList<PublicInStockImageResponse> Images);
