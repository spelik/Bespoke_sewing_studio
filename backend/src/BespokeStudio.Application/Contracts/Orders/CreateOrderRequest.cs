using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Orders;

public sealed record CreateOrderRequest(
    string FullName,
    string? Email,
    string? Phone,
    OrderServiceType? ServiceType,
    Guid? ServiceOfferingId,
    string? ServiceSlug,
    string Description,
    DateOnly? PreferredDate,
    bool Consent,
    IReadOnlyCollection<Guid>? AttachmentIds);
