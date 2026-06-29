using System.Net;
using System.Net.Mail;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Notifications;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class SmtpEmailNotificationSender(
    IOptions<EmailNotificationOptions> options) : IEmailNotificationSender
{
    public Task<EmailNotificationResult> SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            options.Value.Smtp,
            recipientEmail,
            subject,
            body,
            providerName: "Smtp",
            cancellationToken);

    public static async Task<EmailNotificationResult> SendAsync(
        SmtpNotificationOptions smtp,
        string recipientEmail,
        string subject,
        string body,
        string providerName,
        CancellationToken cancellationToken = default)
    {
        var configurationError = smtp.GetConfigurationError();
        if (configurationError is not null)
        {
            throw new InvalidOperationException(configurationError);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromEmail!.Trim(), smtp.FromName.Trim()),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(smtp.Host!.Trim(), smtp.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = smtp.UseSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        }

        await client.SendMailAsync(message, cancellationToken);

        return new EmailNotificationResult(
            Success: true,
            Provider: providerName,
            SentExternally: true,
            Message: "Email was accepted by the configured SMTP server.");
    }
}
