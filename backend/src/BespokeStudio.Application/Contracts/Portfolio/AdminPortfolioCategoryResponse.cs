namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record AdminPortfolioCategoryResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt,
    int ItemCount);
