using BespokeStudio.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class LoggingWhatsAppNotificationSender(
    ILogger<LoggingWhatsAppNotificationSender> logger) : IWhatsAppNotificationSender
{
    public Task SendAsync(
        string recipientPhone,
        string message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Development WhatsApp notification to {RecipientPhone}. Message: {Message}",
            recipientPhone,
            message);

        return Task.CompletedTask;
    }
}
