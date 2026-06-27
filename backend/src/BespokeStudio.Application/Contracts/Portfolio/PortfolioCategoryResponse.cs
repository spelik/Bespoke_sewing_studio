namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record PortfolioCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder,
    bool IsActive);
