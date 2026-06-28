namespace BespokeStudio.Application.Contracts.Services;

public sealed record ServicePriceOptionResponse(
    Guid Id,
    string Label,
    string? Description,
    string PriceText,
    int DisplayOrder,
    bool IsActive);
