namespace BespokeStudio.Application.Abstractions;

public interface IEmailNotificationSender
{
    Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
