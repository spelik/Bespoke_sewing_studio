using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Services;

public sealed record ServiceOfferingResponse(
    Guid Id,
    OrderServiceType ServiceType,
    string Name,
    string ShortDescription,
    string? DetailedDescription,
    decimal? StartingPrice,
    string Currency,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
