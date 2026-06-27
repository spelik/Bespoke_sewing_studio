using BespokeStudio.Application.Contracts.Orders;

namespace BespokeStudio.Application.Abstractions;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderListItemResponse>> GetAllAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> UpdateStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderResponse?> AddNoteAsync(
        Guid orderId,
        AddOrderNoteRequest request,
        CancellationToken cancellationToken = default);
}
