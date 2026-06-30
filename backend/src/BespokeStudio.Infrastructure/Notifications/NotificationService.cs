using System.Text;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.ContactMessages;
using BespokeStudio.Application.Contracts.EmailDeliveryLog;
using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Contracts.Orders;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class NotificationService(
    IOrderService orderService,
    IContactMessageService contactMessageService,
    ISiteSettingsService siteSettingsService,
    IEmailNotificationSender emailSender,
    IEmailDeliveryLogService emailDeliveryLogService,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyNewOrderCreatedAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await orderService.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                logger.LogWarning("Notification skipped because order {OrderId} was not found.", orderId);
                return;
            }

            var settings = await siteSettingsService.GetNotificationSettingsAsync(cancellationToken);

            if (settings.EmailNotificationsEnabled && !string.IsNullOrWhiteSpace(settings.Email))
            {
                var subject = $"New enquiry: {order.ServiceName} from {order.Client.FullName}";
                await SendAndRecordEmailAsync(
                    messageType: "owner_order_notification",
                    recipientEmail: settings.Email,
                    subject: subject,
                    body: BuildOrderEmailBody(order),
                    relatedEntityType: "Order",
                    relatedEntityId: order.Id.ToString(),
                    relatedEntityLabel: order.ReferenceNumber,
                    warningContext: $"Email notification for order {orderId}",
                    cancellationToken: cancellationToken);
            }

            if (settings.CustomerConfirmationEmailsEnabled && !string.IsNullOrWhiteSpace(order.Client.Email))
            {
                var subject = RenderOrderTemplate(settings.CustomerOrderConfirmationSubject, settings.StudioName, order);
                await SendAndRecordEmailAsync(
                    messageType: "customer_order_confirmation",
                    recipientEmail: order.Client.Email,
                    subject: subject,
                    body: RenderOrderTemplate(settings.CustomerOrderConfirmationBody, settings.StudioName, order),
                    relatedEntityType: "Order",
                    relatedEntityId: order.Id.ToString(),
                    relatedEntityLabel: order.ReferenceNumber,
                    warningContext: $"Customer confirmation email for order {orderId}",
                    cancellationToken: cancellationToken);
            }

        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Notifications could not be prepared for order {OrderId}.", orderId);
        }
    }

    public async Task NotifyNewContactMessageCreatedAsync(
        Guid contactMessageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await contactMessageService.GetByIdAsync(contactMessageId, cancellationToken);
            if (message is null)
            {
                logger.LogWarning(
                    "Notification skipped because contact message {ContactMessageId} was not found.",
                    contactMessageId);
                return;
            }

            var settings = await siteSettingsService.GetNotificationSettingsAsync(cancellationToken);

            if (settings.EmailNotificationsEnabled && !string.IsNullOrWhiteSpace(settings.Email))
            {
                var subject = string.IsNullOrWhiteSpace(message.Subject)
                    ? $"New contact message from {message.FullName}"
                    : $"New contact message: {message.Subject}";

                await SendAndRecordEmailAsync(
                    messageType: "owner_contact_notification",
                    recipientEmail: settings.Email,
                    subject: subject,
                    body: BuildContactMessageEmailBody(message),
                    relatedEntityType: "ContactMessage",
                    relatedEntityId: message.Id.ToString(),
                    relatedEntityLabel: message.ReferenceNumber,
                    warningContext: $"Email notification for contact message {contactMessageId}",
                    cancellationToken: cancellationToken);
            }

            if (settings.CustomerConfirmationEmailsEnabled)
            {
                var subject = RenderContactTemplate(settings.CustomerContactConfirmationSubject, settings.StudioName, message);
                await SendAndRecordEmailAsync(
                    messageType: "customer_contact_confirmation",
                    recipientEmail: message.Email,
                    subject: subject,
                    body: RenderContactTemplate(settings.CustomerContactConfirmationBody, settings.StudioName, message),
                    relatedEntityType: "ContactMessage",
                    relatedEntityId: message.Id.ToString(),
                    relatedEntityLabel: message.ReferenceNumber,
                    warningContext: $"Customer confirmation email for contact message {contactMessageId}",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Notifications could not be prepared for contact message {ContactMessageId}.",
                contactMessageId);
        }
    }


    private async Task SendAndRecordEmailAsync(
        string messageType,
        string recipientEmail,
        string subject,
        string body,
        string? relatedEntityType,
        string? relatedEntityId,
        string? relatedEntityLabel,
        string warningContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await emailSender.SendAsync(
                recipientEmail,
                subject,
                body,
                cancellationToken);

            if (!result.Success)
            {
                logger.LogWarning(
                    "{WarningContext} used {Provider}: {Message}",
                    warningContext,
                    result.Provider,
                    result.Message);
            }

            await RecordEmailLogAsync(
                messageType,
                recipientEmail,
                subject,
                result,
                relatedEntityType,
                relatedEntityId,
                relatedEntityLabel,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{WarningContext} failed.", warningContext);
            await RecordEmailLogAsync(
                messageType,
                recipientEmail,
                subject,
                new EmailNotificationResult(
                    Success: false,
                    Provider: "Unknown",
                    SentExternally: false,
                    Message: "Email delivery threw an exception."),
                relatedEntityType,
                relatedEntityId,
                relatedEntityLabel,
                cancellationToken,
                exception.Message);
        }
    }

    private async Task RecordEmailLogAsync(
        string messageType,
        string recipientEmail,
        string subject,
        EmailNotificationResult result,
        string? relatedEntityType,
        string? relatedEntityId,
        string? relatedEntityLabel,
        CancellationToken cancellationToken,
        string? errorMessage = null)
    {
        try
        {
            await emailDeliveryLogService.RecordAsync(
                new EmailDeliveryLogWriteRequest(
                    messageType,
                    recipientEmail,
                    subject,
                    result.Provider,
                    result.Success ? "Sent" : "Failed",
                    result.SentExternally,
                    result.Message,
                    errorMessage,
                    relatedEntityType,
                    relatedEntityId,
                    relatedEntityLabel,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Email delivery log entry could not be saved for {MessageType}.", messageType);
        }
    }

    private static string RenderOrderTemplate(string template, string studioName, OrderResponse order) =>
        RenderTemplate(template, new Dictionary<string, string?>
        {
            ["studioName"] = studioName,
            ["customerName"] = order.Client.FullName,
            ["customerEmail"] = order.Client.Email,
            ["customerPhone"] = order.Client.Phone,
            ["serviceName"] = order.ServiceName,
            ["preferredDate"] = order.PreferredDate?.ToString("yyyy-MM-dd"),
            ["orderReference"] = order.ReferenceNumber
        });

    private static string RenderContactTemplate(string template, string studioName, ContactMessageResponse message) =>
        RenderTemplate(template, new Dictionary<string, string?>
        {
            ["studioName"] = studioName,
            ["customerName"] = message.FullName,
            ["customerEmail"] = message.Email,
            ["customerPhone"] = message.Phone,
            ["messageSubject"] = message.Subject,
            ["contactReference"] = message.ReferenceNumber
        });

    private static string RenderTemplate(string template, IReadOnlyDictionary<string, string?> values)
    {
        var result = template;
        foreach (var (key, value) in values)
        {
            result = result.Replace(
                "{{" + key + "}}",
                string.IsNullOrWhiteSpace(value) ? "Not provided" : value.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string BuildOrderEmailBody(OrderResponse order)
    {
        var body = new StringBuilder()
            .AppendLine($"Client name: {order.Client.FullName}")
            .AppendLine($"Email: {order.Client.Email ?? "Not provided"}")
            .AppendLine($"Phone: {order.Client.Phone ?? "Not provided"}")
            .AppendLine($"Service: {order.ServiceName}")
            .AppendLine($"Preferred date: {order.PreferredDate?.ToString("yyyy-MM-dd") ?? "Not provided"}")
            .AppendLine($"Description: {order.Description}")
            .AppendLine($"Attachment count: {order.Attachments.Count}")
            .AppendLine($"Order reference: {order.ReferenceNumber}")
            .AppendLine($"Created: {order.CreatedAt:O}")
            .AppendLine("Admin: /admin");

        return body.ToString();
    }

    private static string BuildContactMessageEmailBody(ContactMessageResponse message)
    {
        var body = new StringBuilder()
            .AppendLine($"Sender name: {message.FullName}")
            .AppendLine($"Email: {message.Email}")
            .AppendLine($"Phone: {message.Phone ?? "Not provided"}")
            .AppendLine($"Subject: {message.Subject ?? "Not provided"}")
            .AppendLine($"Message: {message.Message}")
            .AppendLine($"Consent given: {(message.ConsentGiven ? "Yes" : "No")}")
            .AppendLine($"Contact message reference: {message.ReferenceNumber}")
            .AppendLine($"Created: {message.CreatedAt:O}")
            .AppendLine("Admin: /admin");

        return body.ToString();
    }
}
