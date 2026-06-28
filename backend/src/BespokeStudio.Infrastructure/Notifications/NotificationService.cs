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
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Notifications could not be prepared for contact message {ContactMessageId}.",
                contactMessageId);
        }
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
            .AppendLine($"Order reference: {order.Id}")
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
            .AppendLine($"Contact message reference: {message.Id}")
            .AppendLine($"Created: {message.CreatedAt:O}")
            .AppendLine("Admin: /admin");

        return body.ToString();
    }
}
