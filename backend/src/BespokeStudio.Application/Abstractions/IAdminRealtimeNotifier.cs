namespace BespokeStudio.Application.Abstractions;

public interface IAdminRealtimeNotifier
{
    Task NotifyOrderCreatedAsync(Guid orderId, string? referenceNumber, CancellationToken cancellationToken = default);

    Task NotifyOrderUpdatedAsync(Guid orderId, string? referenceNumber, CancellationToken cancellationToken = default);

    Task NotifyContactMessageCreatedAsync(Guid contactMessageId, string? referenceNumber, CancellationToken cancellationToken = default);

    Task NotifyContactMessageUpdatedAsync(Guid contactMessageId, string? referenceNumber, CancellationToken cancellationToken = default);
}
