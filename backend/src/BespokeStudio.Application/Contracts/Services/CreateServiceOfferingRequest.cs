using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Services;

public sealed record CreateServiceOfferingRequest(
    OrderServiceType ServiceType,
    string Name,
    string ShortDescription,
    string? DetailedDescription,
    decimal? StartingPrice,
    string Currency,
    bool IsActive,
    int DisplayOrder);
