using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Orders;

public sealed record OrderListQueryRequest(
    int Page,
    int PageSize,
    string? Search,
    OrderStatus? Status);
