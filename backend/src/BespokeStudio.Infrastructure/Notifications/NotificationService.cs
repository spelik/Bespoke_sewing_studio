using System.Text;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.ContactMessages;
using BespokeStudio.Application.Contracts.Orders;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class NotificationService(
    IOrderService orderService,
    IContactMessageService contactMessageService,
    ISiteSettingsService siteSettingsService,
    IEmailNotificationSender emailSender,
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
                try
                {
                    var result = await emailSender.SendAsync(
                        settings.Email,
                        $"New enquiry: {order.ServiceName} from {order.Client.FullName}",
                        BuildOrderEmailBody(order),
                        cancellationToken);

                    if (!result.Success)
                    {
                        logger.LogWarning(
                            "Email notification for order {OrderId} used {Provider}: {Message}",
                            orderId,
                            result.Provider,
                            result.Message);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Email notification failed for order {OrderId}.", orderId);
                }
            }

            if (settings.CustomerConfirmationEmailsEnabled && !string.IsNullOrWhiteSpace(order.Client.Email))
            {
                try
                {
                    var result = await emailSender.SendAsync(
                        order.Client.Email,
                        RenderOrderTemplate(settings.CustomerOrderConfirmationSubject, settings.StudioName, order),
                        RenderOrderTemplate(settings.CustomerOrderConfirmationBody, settings.StudioName, order),
                        cancellationToken);

                    if (!result.Success)
                    {
                        logger.LogWarning(
                            "Customer confirmation email for order {OrderId} used {Provider}: {Message}",
                            orderId,
                            result.Provider,
                            result.Message);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Customer confirmation email failed for order {OrderId}.", orderId);
                }
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
                try
                {
                    var subject = string.IsNullOrWhiteSpace(message.Subject)
                        ? $"New contact message from {message.FullName}"
                        : $"New contact message: {message.Subject}";

                    var result = await emailSender.SendAsync(
                        settings.Email,
                        subject,
                        BuildContactMessageEmailBody(message),
                        cancellationToken);

                    if (!result.Success)
                    {
                        logger.LogWarning(
                            "Email notification for contact message {ContactMessageId} used {Provider}: {Message}",
                            contactMessageId,
                            result.Provider,
                            result.Message);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Email notification failed for contact message {ContactMessageId}.",
                        contactMessageId);
                }
            }

            if (settings.CustomerConfirmationEmailsEnabled)
            {
                try
                {
                    var result = await emailSender.SendAsync(
                        message.Email,
                        RenderContactTemplate(settings.CustomerContactConfirmationSubject, settings.StudioName, message),
                        RenderContactTemplate(settings.CustomerContactConfirmationBody, settings.StudioName, message),
                        cancellationToken);

                    if (!result.Success)
                    {
                        logger.LogWarning(
                            "Customer confirmation email for contact message {ContactMessageId} used {Provider}: {Message}",
                            contactMessageId,
                            result.Provider,
                            result.Message);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Customer confirmation email failed for contact message {ContactMessageId}.",
                        contactMessageId);
                }
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
