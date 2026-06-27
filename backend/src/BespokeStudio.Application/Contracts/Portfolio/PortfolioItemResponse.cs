using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record PortfolioItemResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string? Description,
    PortfolioItemStatus Status,
    Guid? CoverImageFileId,
    int DisplayOrder,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
