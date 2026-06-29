using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class ConfiguredEmailNotificationSender(
    IOptions<EmailNotificationOptions> options,
    IEmailDeliverySettingsService emailDeliverySettingsService,
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
        var deliverySettings = await emailDeliverySettingsService.GetResolvedSettingsAsync(cancellationToken);
        if (deliverySettings.Provider == EmailDeliverySettingsValidator.GmailSmtpProvider)
        {
            return await SendUsingAdminGmailSettingsAsync(
                deliverySettings,
                recipientEmail,
                subject,
                body,
                cancellationToken);
        }

        return await SendUsingConfigurationAsync(
            recipientEmail,
            subject,
            body,
            cancellationToken);
    }

    private async Task<EmailNotificationResult> SendUsingAdminGmailSettingsAsync(
        ResolvedEmailDeliverySettings deliverySettings,
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (deliverySettings.ConfigurationError is not null ||
            string.IsNullOrWhiteSpace(deliverySettings.GmailAddress) ||
            string.IsNullOrWhiteSpace(deliverySettings.AppPassword))
        {
            var reason = deliverySettings.ConfigurationError ?? "Gmail SMTP settings are incomplete.";
            logger.LogWarning("Admin-managed Gmail SMTP is not fully configured: {ConfigurationError}", reason);
            return await LogFallbackAsync(recipientEmail, subject, body, reason, cancellationToken);
        }

        var smtp = new SmtpNotificationOptions
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = deliverySettings.GmailAddress,
            Password = deliverySettings.AppPassword,
            FromEmail = deliverySettings.GmailAddress,
            FromName = deliverySettings.SenderName,
            UseSsl = true
        };

        try
        {
            return await SmtpEmailNotificationSender.SendAsync(
                smtp,
                recipientEmail,
                subject,
                body,
                EmailDeliverySettingsValidator.GmailSmtpProvider,
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
                "Admin-managed Gmail SMTP delivery failed for recipient {RecipientEmail}; using logging fallback.",
                recipientEmail);
            return await LogFallbackAsync(
                recipientEmail,
                subject,
                body,
                "Gmail SMTP delivery failed.",
                cancellationToken);
        }
    }

    private async Task<EmailNotificationResult> SendUsingConfigurationAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
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
