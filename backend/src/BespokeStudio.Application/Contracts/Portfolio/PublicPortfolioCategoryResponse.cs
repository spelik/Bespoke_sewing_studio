namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record PublicPortfolioCategoryResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    int DisplayOrder);
