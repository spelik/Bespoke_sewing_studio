using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Orders;

public sealed record UpdateOrderStatusRequest(OrderStatus Status);
