namespace BespokeStudio.Application.Abstractions;

public interface INotificationService
{
    Task NotifyNewOrderCreatedAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task NotifyNewContactMessageCreatedAsync(
        Guid contactMessageId,
        CancellationToken cancellationToken = default);
}
