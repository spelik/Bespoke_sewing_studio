using System.Text;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Orders;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class NotificationService(
    IOrderService orderService,
    ISiteSettingsService siteSettingsService,
    IEmailNotificationSender emailSender,
    IWhatsAppNotificationSender whatsAppSender,
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
                    await emailSender.SendAsync(
                        settings.Email,
                        $"New enquiry: {order.ServiceType} from {order.Client.FullName}",
                        BuildEmailBody(order),
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Email notification failed for order {OrderId}.", orderId);
                }
            }

            if (settings.WhatsAppNotificationsEnabled && !string.IsNullOrWhiteSpace(settings.Phone))
            {
                try
                {
                    await whatsAppSender.SendAsync(
                        settings.Phone,
                        BuildWhatsAppMessage(order),
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "WhatsApp notification failed for order {OrderId}.", orderId);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Notifications could not be prepared for order {OrderId}.", orderId);
        }
    }

    private static string BuildEmailBody(OrderResponse order)
    {
        var body = new StringBuilder()
            .AppendLine($"Client name: {order.Client.FullName}")
            .AppendLine($"Email: {order.Client.Email ?? "Not provided"}")
            .AppendLine($"Phone: {order.Client.Phone ?? "Not provided"}")
            .AppendLine($"Service type: {order.ServiceType}")
            .AppendLine($"Preferred date: {order.PreferredDate?.ToString("yyyy-MM-dd") ?? "Not provided"}")
            .AppendLine($"Description: {order.Description}")
            .AppendLine($"Attachment count: {order.Attachments.Count}")
            .AppendLine($"Admin order: /admin (order {order.Id})");

        return body.ToString();
    }

    private static string BuildWhatsAppMessage(OrderResponse order) =>
        $"New enquiry from {order.Client.FullName}{Environment.NewLine}" +
        $"Service: {order.ServiceType}{Environment.NewLine}" +
        $"Phone: {order.Client.Phone ?? "Not provided"}{Environment.NewLine}" +
        $"Email: {order.Client.Email ?? "Not provided"}{Environment.NewLine}" +
        $"Message: {Shorten(order.Description, 240)}";

    private static string Shorten(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..(maxLength - 3)]}...";
}
