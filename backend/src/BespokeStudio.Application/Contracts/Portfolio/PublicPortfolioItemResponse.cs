namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record PublicPortfolioItemResponse(
    Guid Id,
    string? Slug,
    string Title,
    string? ShortDescription,
    string? Description,
    PublicPortfolioCategoryResponse Category,
    string ImageUrl,
    string AltText,
    bool IsFeatured,
    int DisplayOrder);
