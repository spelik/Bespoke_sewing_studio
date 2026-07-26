namespace BespokeStudio.Application.Contracts.InStock;

public sealed record UpdateInStockImageRequest(
    string? AltText,
    int DisplayOrder);
