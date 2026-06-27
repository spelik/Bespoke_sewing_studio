using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Orders;

public sealed record OrderListItemResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    OrderServiceType ServiceType,
    OrderStatus Status,
    DateOnly? PreferredDate,
    DateTimeOffset CreatedAt);
