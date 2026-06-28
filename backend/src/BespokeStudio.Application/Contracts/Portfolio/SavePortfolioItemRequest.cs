namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record SavePortfolioItemRequest(
    Guid CategoryId,
    string? Slug,
    string Title,
    string? ShortDescription,
    string? Description,
    Guid? ImageFileId,
    string? AltText,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder);
