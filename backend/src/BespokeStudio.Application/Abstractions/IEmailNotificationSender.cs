using BespokeStudio.Application.Contracts.Notifications;

namespace BespokeStudio.Application.Abstractions;

public interface IEmailNotificationSender
{
    Task<EmailNotificationResult> SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
