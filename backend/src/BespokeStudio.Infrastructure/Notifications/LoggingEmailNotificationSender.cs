using BespokeStudio.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class LoggingEmailNotificationSender(
    ILogger<LoggingEmailNotificationSender> logger) : IEmailNotificationSender
{
    public Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Development email notification to {RecipientEmail}. Subject: {Subject}. Body: {Body}",
            recipientEmail,
            subject,
            body);

        return Task.CompletedTask;
    }
}
