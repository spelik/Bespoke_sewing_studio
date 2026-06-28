using System.Text;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Orders;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class NotificationService(
    IOrderService orderService,
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
                        BuildEmailBody(order),
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

    private static string BuildEmailBody(OrderResponse order)
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
}
