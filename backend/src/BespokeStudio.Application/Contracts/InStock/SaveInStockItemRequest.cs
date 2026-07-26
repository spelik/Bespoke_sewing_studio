using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.InStock;

public sealed record SaveInStockItemRequest(
    string? Slug,
    string Title,
    string? ShortDescription,
    string? Description,
    decimal Price,
    string? Currency,
    InStockItemStatus Status,
    bool IsPublished,
    int DisplayOrder,
    string? Sizes,
    string? Materials);
