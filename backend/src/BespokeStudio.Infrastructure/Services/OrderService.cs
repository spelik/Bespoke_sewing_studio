using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Clients;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Validation;
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
        var serviceOffering = await ResolveServiceOfferingAsync(request, cancellationToken);
        var legacyServiceType = request.ServiceType ?? OrderServiceType.Other;
        var serviceName = serviceOffering?.Name ?? FormatLegacyServiceName(legacyServiceType);
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedPhone = NormalizeOptional(request.Phone);
        var attachmentIds = request.AttachmentIds?.ToArray() ?? [];
        UploadedFileMetadata[] uploadedFiles = attachmentIds.Length == 0
            ? []
            : await dbContext.UploadedFiles
                .AsNoTracking()
                .Where(file =>
                    attachmentIds.Contains(file.Id) &&
                    file.Purpose == UploadPurpose.OrderAttachment)
                .ToArrayAsync(cancellationToken);

        if (uploadedFiles.Length != attachmentIds.Length)
        {
            throw new OrderAttachmentValidationException(
                "One or more uploaded attachments are missing or invalid.");
        }

        if (uploadedFiles.Any(file => file.ScanStatus is not (UploadScanStatus.Clean or UploadScanStatus.Skipped)))
        {
            throw new OrderAttachmentValidationException(
                "One or more uploaded attachments have not passed security checks.");
        }

        if (attachmentIds.Length > 0 && await dbContext.OrderAttachments
                .AsNoTracking()
                .AnyAsync(attachment => attachmentIds.Contains(attachment.UploadedFileId), cancellationToken))
        {
            throw new OrderAttachmentValidationException(
                "One or more uploaded attachments are already linked to an order.");
        }

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
            ServiceType = legacyServiceType,
            ServiceOfferingId = serviceOffering?.Id,
            ServiceNameSnapshot = serviceName,
            Status = OrderStatus.New,
            Description = request.Description.Trim(),
            PreferredDate = request.PreferredDate,
            ConsentGiven = request.Consent,
            ConsentRecordedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Orders.Add(order);
        var uploadedFilesById = uploadedFiles.ToDictionary(file => file.Id);
        var orderAttachments = attachmentIds.Select((uploadedFileId, index) => new OrderAttachment
        {
            OrderId = order.Id,
            UploadedFileId = uploadedFileId,
            DisplayOrder = index,
            CreatedAt = now
        }).ToArray();
        dbContext.OrderAttachments.AddRange(orderAttachments);
        await dbContext.SaveChangesAsync(cancellationToken);

        var attachmentResponses = orderAttachments.Select(attachment =>
        {
            var file = uploadedFilesById[attachment.UploadedFileId];
            return new OrderAttachmentResponse(
                attachment.Id,
                file.Id,
                file.OriginalFileName,
                file.ContentType,
                file.SizeBytes,
                attachment.Caption,
                attachment.DisplayOrder,
                file.ScanStatus,
                file.ScanProvider,
                file.ScannedAt);
        }).ToArray();

        return MapOrderResponse(order, client, attachmentResponses, []);
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
                attachment.DisplayOrder,
                file.ScanStatus,
                file.ScanProvider,
                file.ScannedAt))
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
                client.Email,
                client.Phone,
                order.ServiceOfferingId,
                order.ServiceNameSnapshot,
                order.ServiceType,
                order.Status,
                order.Description,
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
            order.ServiceOfferingId,
            order.ServiceNameSnapshot,
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

    private async Task<ServiceOffering?> ResolveServiceOfferingAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        ServiceOffering? service = null;
        if (request.ServiceOfferingId.HasValue)
        {
            service = await dbContext.ServiceOfferings.AsNoTracking().SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.ServiceOfferingId.Value &&
                    candidate.IsActive &&
                    candidate.ArchivedAt == null,
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.ServiceSlug))
        {
            var slug = request.ServiceSlug.Trim();
            service = await dbContext.ServiceOfferings.AsNoTracking().SingleOrDefaultAsync(
                candidate =>
                    candidate.Slug == slug &&
                    candidate.IsActive &&
                    candidate.ArchivedAt == null,
                cancellationToken);
        }

        if ((request.ServiceOfferingId.HasValue || !string.IsNullOrWhiteSpace(request.ServiceSlug)) && service is null)
        {
            throw new OrderServiceSelectionException(
                request.ServiceOfferingId.HasValue ? "ServiceOfferingId" : "ServiceSlug",
                "The selected service is unavailable. Refresh the service list and choose an active service.");
        }

        return service;
    }

    private static string FormatLegacyServiceName(OrderServiceType serviceType) =>
        serviceType == OrderServiceType.MemoryBear ? "Memory Bears" : serviceType.ToString();

    private static string? NormalizeEmail(string? email)
    {
        return NormalizeOptional(email)?.ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
