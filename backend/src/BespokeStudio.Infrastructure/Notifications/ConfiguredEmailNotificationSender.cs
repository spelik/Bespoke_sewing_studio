using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class ConfiguredEmailNotificationSender(
    IOptions<EmailNotificationOptions> options,
    LoggingEmailNotificationSender loggingSender,
    SmtpEmailNotificationSender smtpSender,
    ILogger<ConfiguredEmailNotificationSender> logger) : IEmailNotificationSender
{
    public async Task<EmailNotificationResult> SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var provider = options.Value.Provider.Trim();

        if (provider.Equals("Logging", StringComparison.OrdinalIgnoreCase))
        {
            return await loggingSender.SendAsync(
                recipientEmail,
                subject,
                body,
                cancellationToken);
        }

        if (!provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Unknown email notification provider {Provider}; using logging fallback.",
                provider);
            return await LogFallbackAsync(
                recipientEmail,
                subject,
                body,
                "The configured email provider is unknown.",
                cancellationToken);
        }

        var configurationError = options.Value.Smtp.GetConfigurationError();
        if (configurationError is not null)
        {
            logger.LogWarning(
                "SMTP email notification is not fully configured: {ConfigurationError} Using logging fallback.",
                configurationError);
            return await LogFallbackAsync(
                recipientEmail,
                subject,
                body,
                configurationError,
                cancellationToken);
        }

        try
        {
            return await smtpSender.SendAsync(
                recipientEmail,
                subject,
                body,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "SMTP email delivery failed for recipient {RecipientEmail}; using logging fallback.",
                recipientEmail);
            return await LogFallbackAsync(
                recipientEmail,
                subject,
                body,
                "SMTP delivery failed.",
                cancellationToken);
        }
    }

    private async Task<EmailNotificationResult> LogFallbackAsync(
        string recipientEmail,
        string subject,
        string body,
        string reason,
        CancellationToken cancellationToken)
    {
        await loggingSender.SendAsync(
            recipientEmail,
            subject,
            body,
            cancellationToken);

        return new EmailNotificationResult(
            Success: false,
            Provider: "LoggingFallback",
            SentExternally: false,
            Message: $"{reason} Email content was written to the application log.");
    }
}
