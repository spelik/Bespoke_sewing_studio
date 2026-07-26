using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.InStock;

public sealed record AdminInStockItemResponse(
    Guid Id,
    string Slug,
    string Title,
    string? ShortDescription,
    string? Description,
    decimal Price,
    string Currency,
    InStockItemStatus Status,
    bool IsPublished,
    int DisplayOrder,
    string? Sizes,
    string? Materials,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt,
    IReadOnlyList<AdminInStockImageResponse> Images);
