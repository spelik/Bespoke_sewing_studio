namespace BespokeStudio.Application.Contracts.InStock;

public sealed record PublicInStockImageResponse(
    Guid Id,
    string ImageUrl,
    string? AltText,
    int DisplayOrder);
