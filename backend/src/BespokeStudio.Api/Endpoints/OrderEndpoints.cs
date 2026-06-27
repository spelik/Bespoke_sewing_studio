using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var orders = endpoints.MapGroup("/api/orders")
            .WithTags("Orders");

        orders.MapPost(string.Empty, CreateOrderAsync)
            .WithName("CreateOrder")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        orders.MapGet(string.Empty, GetOrdersAsync)
            .WithName("GetOrders")
            .Produces<IReadOnlyList<OrderListItemResponse>>();

        orders.MapGet("/{id:guid}", GetOrderByIdAsync)
            .WithName("GetOrderById")
            .Produces<OrderResponse>()
            .Produces(StatusCodes.Status404NotFound);

        orders.MapPatch("/{id:guid}/status", UpdateOrderStatusAsync)
            .WithName("UpdateOrderStatus")
            .Produces<OrderResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        orders.MapPost("/{id:guid}/notes", AddOrderNoteAsync)
            .WithName("AddOrderNote")
            .Produces<OrderResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var errors = OrderRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var order = await orderService.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/orders/{order.Id}", order);
    }

    private static async Task<IResult> GetOrdersAsync(
        int? take,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var limit = take ?? 100;

        if (limit is < 1 or > 200)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["take"] = ["Take must be between 1 and 200."]
            });
        }

        var orders = await orderService.GetAllAsync(limit, cancellationToken);
        return TypedResults.Ok(orders);
    }

    private static async Task<IResult> GetOrderByIdAsync(
        Guid id,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var order = await orderService.GetByIdAsync(id, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
    }

    private static async Task<IResult> UpdateOrderStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var errors = OrderRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var order = await orderService.UpdateStatusAsync(id, request, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
    }

    private static async Task<IResult> AddOrderNoteAsync(
        Guid id,
        AddOrderNoteRequest request,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var errors = OrderRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var order = await orderService.AddNoteAsync(id, request, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
    }

    private static Dictionary<string, string[]> ToJsonPropertyNames(
        IReadOnlyDictionary<string, string[]> errors)
    {
        return errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
    }
}
