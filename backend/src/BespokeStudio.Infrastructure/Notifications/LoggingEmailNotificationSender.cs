using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class LoggingEmailNotificationSender(
    ILogger<LoggingEmailNotificationSender> logger) : IEmailNotificationSender
{
    public Task<EmailNotificationResult> SendAsync(
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

        return Task.FromResult(new EmailNotificationResult(
            Success: true,
            Provider: "Logging",
            SentExternally: false,
            Message: "Email notification was written to the application log."));
    }
}
