using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminRealtime;
using Microsoft.AspNetCore.SignalR;

namespace BespokeStudio.Api.Hubs;

public sealed class SignalRAdminRealtimeNotifier(
    IHubContext<AdminNotificationsHub> hubContext,
    ILogger<SignalRAdminRealtimeNotifier> logger) : IAdminRealtimeNotifier
{
    public Task NotifyOrderCreatedAsync(
        Guid orderId,
        string? referenceNumber,
        CancellationToken cancellationToken = default) =>
        BroadcastAsync("OrderCreated", "Order", orderId, referenceNumber, cancellationToken);

    public Task NotifyOrderUpdatedAsync(
        Guid orderId,
        string? referenceNumber,
        CancellationToken cancellationToken = default) =>
        BroadcastAsync("OrderUpdated", "Order", orderId, referenceNumber, cancellationToken);

    public Task NotifyContactMessageCreatedAsync(
        Guid contactMessageId,
        string? referenceNumber,
        CancellationToken cancellationToken = default) =>
        BroadcastAsync("ContactMessageCreated", "ContactMessage", contactMessageId, referenceNumber, cancellationToken);

    public Task NotifyContactMessageUpdatedAsync(
        Guid contactMessageId,
        string? referenceNumber,
        CancellationToken cancellationToken = default) =>
        BroadcastAsync("ContactMessageUpdated", "ContactMessage", contactMessageId, referenceNumber, cancellationToken);

    private async Task BroadcastAsync(
        string eventType,
        string entity,
        Guid entityId,
        string? referenceNumber,
        CancellationToken cancellationToken)
    {
        var payload = new AdminRealtimeEvent(
            eventType,
            entity,
            entityId,
            referenceNumber,
            DateTimeOffset.UtcNow);

        try
        {
            await hubContext.Clients.All.SendAsync("AdminDataChanged", payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Admin realtime broadcast failed for {EventType} {EntityId}.",
                eventType,
                entityId);
        }
    }
}
