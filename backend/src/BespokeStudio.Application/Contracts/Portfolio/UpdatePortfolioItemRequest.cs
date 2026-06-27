using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record UpdatePortfolioItemRequest(
    Guid CategoryId,
    string Title,
    string? Description,
    PortfolioItemStatus Status,
    Guid? CoverImageFileId,
    int DisplayOrder);
