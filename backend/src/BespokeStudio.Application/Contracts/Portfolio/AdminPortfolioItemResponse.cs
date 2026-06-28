namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record AdminPortfolioItemResponse(
    Guid Id,
    string? Slug,
    string Title,
    string? ShortDescription,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    Guid? ImageFileId,
    string? ImageUrl,
    string AltText,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);
