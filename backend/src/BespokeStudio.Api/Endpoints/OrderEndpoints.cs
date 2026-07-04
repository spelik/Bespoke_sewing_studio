using System.Security.Claims;
using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Common;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;
using BespokeStudio.Api.Configuration;
using BespokeStudio.Domain.Enums;

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
            .Produces<PagedResponse<OrderListItemResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        orders.MapGet("/{id:guid}", GetOrderByIdAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("GetOrderById")
            .Produces<OrderResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        orders.MapDelete("/{id:guid}", DeleteOrderAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("DeleteOrder")
            .Produces<DeleteOrderResult>()
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

        orders.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", DeleteOrderAttachmentAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("DeleteOrderAttachment")
            .Produces<OrderResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        IOrderService orderService,
        INotificationService notificationService,
        IAdminRealtimeNotifier realtimeNotifier,
        ILoggerFactory loggerFactory,
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
            await RunPostCommitSideEffectsAsync(
                order,
                notificationService,
                realtimeNotifier,
                loggerFactory);
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

    private static async Task RunPostCommitSideEffectsAsync(
        OrderResponse order,
        INotificationService notificationService,
        IAdminRealtimeNotifier realtimeNotifier,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("OrderCreationSideEffects");

        try
        {
            await notificationService.NotifyNewOrderCreatedAsync(
                order.Id,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            TryLogPostCommitFailure(
                logger,
                exception,
                "Post-commit notifications failed for order {OrderId} ({ReferenceNumber}).",
                order);
        }

        try
        {
            await realtimeNotifier.NotifyOrderCreatedAsync(
                order.Id,
                order.ReferenceNumber,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            TryLogPostCommitFailure(
                logger,
                exception,
                "Post-commit realtime notification failed for order {OrderId} ({ReferenceNumber}).",
                order);
        }
    }

    private static void TryLogPostCommitFailure(
        ILogger logger,
        Exception exception,
        string message,
        OrderResponse order)
    {
        try
        {
            logger.LogWarning(
                exception,
                message,
                order.Id,
                order.ReferenceNumber);
        }
        catch
        {
            // A failing logging provider must not turn a persisted order into HTTP 500.
        }
    }

    private static async Task<IResult> GetOrdersAsync(
        int? page,
        int? pageSize,
        string? search,
        OrderStatus? status,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var pagination = PaginationQuery.Normalize(page, pageSize);

        var orders = await orderService.GetPageAsync(
            new OrderListQueryRequest(
                pagination.Page,
                pagination.PageSize,
                search,
                status),
            cancellationToken);
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


    private static async Task<IResult> DeleteOrderAsync(
        Guid id,
        ClaimsPrincipal principal,
        IOrderService orderService,
        IAdminRealtimeNotifier realtimeNotifier,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var deleted = await orderService.DeleteAsync(
            id,
            AdminAuditEndpointHelpers.GetActor(principal),
            cancellationToken);
        if (deleted is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            await realtimeNotifier.NotifyOrderDeletedAsync(
                deleted.Id,
                deleted.ReferenceNumber,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("AdminDeleteOperations").LogWarning(
                exception,
                "Post-commit realtime notification failed for deleted order {OrderId}.",
                deleted.Id);
        }

        return TypedResults.Ok(deleted);
    }

    private static async Task<IResult> UpdateOrderStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        ClaimsPrincipal principal,
        IOrderService orderService,
        IAdminRealtimeNotifier realtimeNotifier,
        IAdminAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var errors = OrderRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var order = await orderService.UpdateStatusAsync(id, request, cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        await realtimeNotifier.NotifyOrderUpdatedAsync(order.Id, order.ReferenceNumber, cancellationToken);
        await auditLogService.RecordAsync(
            AdminAuditEndpointHelpers.CreateAuditRequest(
                principal,
                "order.status_updated",
                "Order",
                order.Id.ToString(),
                order.ReferenceNumber,
                $"Order {order.ReferenceNumber} status was set to {order.Status}."),
            cancellationToken);
        return TypedResults.Ok(order);
    }

    private static async Task<IResult> AddOrderNoteAsync(
        Guid id,
        AddOrderNoteRequest request,
        ClaimsPrincipal principal,
        IOrderService orderService,
        IAdminRealtimeNotifier realtimeNotifier,
        IAdminAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var errors = OrderRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var order = await orderService.AddNoteAsync(id, request, cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        await realtimeNotifier.NotifyOrderUpdatedAsync(order.Id, order.ReferenceNumber, cancellationToken);
        await auditLogService.RecordAsync(
            AdminAuditEndpointHelpers.CreateAuditRequest(
                principal,
                "order.note_added",
                "Order",
                order.Id.ToString(),
                order.ReferenceNumber,
                $"A note was added to order {order.ReferenceNumber}."),
            cancellationToken);
        return TypedResults.Ok(order);
    }


    private static async Task<IResult> DeleteOrderAttachmentAsync(
        Guid id,
        Guid attachmentId,
        ClaimsPrincipal principal,
        IUploadService uploadService,
        IOrderService orderService,
        IAdminRealtimeNotifier realtimeNotifier,
        IAdminAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var deletion = await uploadService.DeleteOrderAttachmentAsync(id, attachmentId, cancellationToken);
        if (deletion is null)
        {
            return TypedResults.NotFound();
        }

        var order = await orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        await realtimeNotifier.NotifyOrderUpdatedAsync(order.Id, order.ReferenceNumber, cancellationToken);
        await auditLogService.RecordAsync(
            AdminAuditEndpointHelpers.CreateAuditRequest(
                principal,
                "order_attachment.deleted",
                "Order",
                order.Id.ToString(),
                order.ReferenceNumber,
                $"Attachment '{deletion.OriginalFileName}' was removed from order {order.ReferenceNumber}."),
            cancellationToken);

        return TypedResults.Ok(order);
    }

    private static Dictionary<string, string[]> ToJsonPropertyNames(
        IReadOnlyDictionary<string, string[]> errors)
    {
        return errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
    }
}
