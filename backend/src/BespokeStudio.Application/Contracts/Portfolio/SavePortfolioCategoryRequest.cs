namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record SavePortfolioCategoryRequest(
    string? Slug,
    string Name,
    string? Description,
    bool IsActive,
    int DisplayOrder);
