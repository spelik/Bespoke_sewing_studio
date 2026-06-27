using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Orders;

public sealed record OrderListItemResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string? ClientEmail,
    string? ClientPhone,
    OrderServiceType ServiceType,
    OrderStatus Status,
    string Description,
    DateOnly? PreferredDate,
    DateTimeOffset CreatedAt);
