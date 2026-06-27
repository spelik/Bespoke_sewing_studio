using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Clients;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed class OrderService(BespokeStudioDbContext dbContext) : IOrderService
{
    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedPhone = NormalizeOptional(request.Phone);

        Client? client = null;

        if (normalizedEmail is not null)
        {
            client = await dbContext.Clients.FirstOrDefaultAsync(
                candidate => candidate.Email != null && candidate.Email.ToLower() == normalizedEmail,
                cancellationToken);
        }

        if (client is null && normalizedPhone is not null)
        {
            client = await dbContext.Clients.FirstOrDefaultAsync(
                candidate => candidate.Phone == normalizedPhone,
                cancellationToken);
        }

        if (client is null)
        {
            client = new Client
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                Phone = normalizedPhone,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Clients.Add(client);
        }
        else
        {
            var clientChanged = false;

            if (client.Email is null && normalizedEmail is not null)
            {
                client.Email = normalizedEmail;
                clientChanged = true;
            }

            if (client.Phone is null && normalizedPhone is not null)
            {
                client.Phone = normalizedPhone;
                clientChanged = true;
            }

            if (clientChanged)
            {
                client.UpdatedAt = now;
            }
        }

        var order = new Order
        {
            ClientId = client.Id,
            ServiceType = request.ServiceType,
            Status = OrderStatus.New,
            Description = request.Description.Trim(),
            PreferredDate = request.PreferredDate,
            ConsentGiven = request.Consent,
            ConsentRecordedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapOrderResponse(order, client, [], []);
    }

    public async Task<OrderResponse?> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var orderWithClient = await (
            from order in dbContext.Orders.AsNoTracking()
            join client in dbContext.Clients.AsNoTracking() on order.ClientId equals client.Id
            where order.Id == orderId
            select new { Order = order, Client = client })
            .SingleOrDefaultAsync(cancellationToken);

        if (orderWithClient is null)
        {
            return null;
        }

        var attachments = await (
            from attachment in dbContext.OrderAttachments.AsNoTracking()
            join file in dbContext.UploadedFiles.AsNoTracking()
                on attachment.UploadedFileId equals file.Id
            where attachment.OrderId == orderId
            orderby attachment.DisplayOrder, attachment.CreatedAt
            select new OrderAttachmentResponse(
                attachment.Id,
                attachment.UploadedFileId,
                file.OriginalFileName,
                file.ContentType,
                file.SizeBytes,
                attachment.Caption,
                attachment.DisplayOrder))
            .ToListAsync(cancellationToken);

        var notes = await dbContext.OrderNotes
            .AsNoTracking()
            .Where(note => note.OrderId == orderId)
            .OrderBy(note => note.CreatedAt)
            .Select(note => new OrderNoteResponse(note.Id, note.Text, note.CreatedAt))
            .ToListAsync(cancellationToken);

        return MapOrderResponse(
            orderWithClient.Order,
            orderWithClient.Client,
            attachments,
            notes);
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> GetAllAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await (
            from order in dbContext.Orders.AsNoTracking()
            join client in dbContext.Clients.AsNoTracking() on order.ClientId equals client.Id
            orderby order.CreatedAt descending
            select new OrderListItemResponse(
                order.Id,
                order.ClientId,
                client.FullName,
                order.ServiceType,
                order.Status,
                order.PreferredDate,
                order.CreatedAt))
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderResponse?> UpdateStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(
            candidate => candidate.Id == orderId,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(orderId, cancellationToken);
    }

    public async Task<OrderResponse?> AddNoteAsync(
        Guid orderId,
        AddOrderNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(
            candidate => candidate.Id == orderId,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        dbContext.OrderNotes.Add(new OrderNote
        {
            OrderId = orderId,
            Text = request.Text.Trim(),
            CreatedAt = now
        });

        order.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(orderId, cancellationToken);
    }

    private static OrderResponse MapOrderResponse(
        Order order,
        Client client,
        IReadOnlyCollection<OrderAttachmentResponse> attachments,
        IReadOnlyCollection<OrderNoteResponse> notes)
    {
        return new OrderResponse(
            order.Id,
            order.ClientId,
            new ClientResponse(
                client.Id,
                client.FullName,
                client.Email,
                client.Phone,
                client.CreatedAt,
                client.UpdatedAt),
            order.ServiceType,
            order.Status,
            order.Description,
            order.PreferredDate,
            order.ConsentGiven,
            order.ConsentRecordedAt,
            order.QuotedAmount,
            order.Currency,
            order.CreatedAt,
            order.UpdatedAt,
            attachments,
            notes);
    }

    private static string? NormalizeEmail(string? email)
    {
        return NormalizeOptional(email)?.ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
