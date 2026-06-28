using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Security;

namespace BespokeStudio.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var notifications = endpoints.MapGroup("/api/admin/notifications")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Notifications");

        notifications.MapPost("/test-email", SendTestEmailAsync)
            .WithName("SendTestEmailNotification")
            .Produces<EmailNotificationResult>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> SendTestEmailAsync(
        ISiteSettingsService settingsService,
        IEmailNotificationSender emailSender,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetNotificationSettingsAsync(cancellationToken);
        if (!settings.EmailNotificationsEnabled)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["emailNotificationsEnabled"] =
                    ["Enable email notifications in Site Settings before sending a test email."]
            });
        }

        if (string.IsNullOrWhiteSpace(settings.Email))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["A destination email is required in Site Settings."]
            });
        }

        var result = await emailSender.SendAsync(
            settings.Email,
            "Bespoke Sewing Studio test email",
            $"This is a test notification from Bespoke Sewing Studio.{Environment.NewLine}" +
            $"Generated: {DateTimeOffset.UtcNow:O}{Environment.NewLine}" +
            "If the SMTP provider is active, email delivery configuration is working.",
            cancellationToken);

        return TypedResults.Ok(result);
    }
}
