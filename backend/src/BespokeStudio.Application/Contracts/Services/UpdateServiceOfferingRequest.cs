using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Services;

public sealed record UpdateServiceOfferingRequest(
    OrderServiceType ServiceType,
    string Name,
    string ShortDescription,
    string? DetailedDescription,
    decimal? StartingPrice,
    string Currency,
    bool IsActive,
    int DisplayOrder);
