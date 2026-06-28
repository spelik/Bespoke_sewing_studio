using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;
using BespokeStudio.Api.Configuration;

namespace BespokeStudio.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var orders = endpoints.MapGroup("/api/orders")
            .WithTags("Orders");

        orders.MapPost(string.Empty, CreateOrderAsync)
            .RequireRateLimiting(RateLimitPolicies.PublicOrder)
            .WithName("CreateOrder")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status429TooManyRequests);

        orders.MapGet(string.Empty, GetOrdersAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("GetOrders")
            .Produces<IReadOnlyList<OrderListItemResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        orders.MapGet("/{id:guid}", GetOrderByIdAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("GetOrderById")
            .Produces<OrderResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        orders.MapPatch("/{id:guid}/status", UpdateOrderStatusAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("UpdateOrderStatus")
            .Produces<OrderResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        orders.MapPost("/{id:guid}/notes", AddOrderNoteAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("AddOrderNote")
            .Produces<OrderResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        IOrderService orderService,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var errors = OrderRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        try
        {
            var order = await orderService.CreateAsync(request, cancellationToken);
            await notificationService.NotifyNewOrderCreatedAsync(order.Id, cancellationToken);
            return TypedResults.Created($"/api/orders/{order.Id}", order);
        }
        catch (OrderAttachmentValidationException exception)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["attachmentIds"] = [exception.Message]
            });
        }
        catch (OrderServiceSelectionException exception)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [JsonNamingPolicy.CamelCase.ConvertName(exception.Field)] = [exception.Message]
            });
        }
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
