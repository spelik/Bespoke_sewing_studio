namespace BespokeStudio.Application.Contracts.Services;

public sealed record ServicePriceOptionRequest(
    string Label,
    string? Description,
    string PriceText,
    int DisplayOrder,
    bool IsActive);
