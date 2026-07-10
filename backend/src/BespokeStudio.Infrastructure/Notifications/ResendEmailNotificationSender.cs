using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Validation;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class ResendEmailNotificationSender(
    HttpClient httpClient,
    ILogger<ResendEmailNotificationSender> logger)
{
    public async Task<EmailNotificationResult> SendAsync(
        ResolvedEmailDeliverySettings settings,
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (settings.ConfigurationError is not null ||
            string.IsNullOrWhiteSpace(settings.ResendApiKey) ||
            string.IsNullOrWhiteSpace(settings.ResendFromEmail) ||
            string.IsNullOrWhiteSpace(settings.ReplyToEmail))
        {
            throw new InvalidOperationException(
                settings.ConfigurationError ?? "Resend API settings are incomplete.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            settings.ResendApiKey);
        request.Content = JsonContent.Create(new ResendSendEmailRequest(
            From: $"{settings.SenderName} <{settings.ResendFromEmail}>",
            To: [recipientEmail],
            Subject: subject,
            Text: body,
            ReplyTo: [settings.ReplyToEmail]));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ResendSendEmailResponse>(
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Resend API delivery failed with HTTP {StatusCode}.",
                (int)response.StatusCode);
            throw new InvalidOperationException("Resend API rejected the email.");
        }

        var messageId = string.IsNullOrWhiteSpace(result?.Id)
            ? "unknown"
            : result.Id.Trim();

        return new EmailNotificationResult(
            Success: true,
            Provider: EmailDeliverySettingsValidator.ResendApiProvider,
            SentExternally: true,
            Message: $"Resend accepted the email. Message id: {messageId}.");
    }

    private sealed record ResendSendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] IReadOnlyList<string> To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("reply_to")] IReadOnlyList<string> ReplyTo);

    private sealed record ResendSendEmailResponse(
        [property: JsonPropertyName("id")] string? Id);
}
