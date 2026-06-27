namespace BespokeStudio.Application.Abstractions;

public interface IWhatsAppNotificationSender
{
    Task SendAsync(
        string recipientPhone,
        string message,
        CancellationToken cancellationToken = default);
}
