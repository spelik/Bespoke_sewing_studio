namespace BespokeStudio.Application.Abstractions;

public interface INotificationService
{
    Task NotifyNewOrderCreatedAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}
