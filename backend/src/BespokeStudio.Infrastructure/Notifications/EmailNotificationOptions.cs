using System.Net.Mail;

namespace BespokeStudio.Infrastructure.Notifications;

public sealed class EmailNotificationOptions
{
    public const string SectionName = "Notifications:Email";

    public string Provider { get; set; } = "Logging";
    public SmtpNotificationOptions Smtp { get; set; } = new();
}

public sealed class SmtpNotificationOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromEmail { get; set; }
    public string FromName { get; set; } = "Bespoke Sewing Studio";
    public bool UseSsl { get; set; } = true;

    public string? GetConfigurationError()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            return "SMTP host is not configured.";
        }

        if (Port is < 1 or > 65535)
        {
            return "SMTP port must be between 1 and 65535.";
        }

        if (string.IsNullOrWhiteSpace(FromEmail) ||
            !MailAddress.TryCreate(FromEmail.Trim(), out _))
        {
            return "SMTP from email is missing or invalid.";
        }

        if (string.IsNullOrWhiteSpace(Username) != string.IsNullOrWhiteSpace(Password))
        {
            return "SMTP username and password must either both be configured or both be omitted.";
        }

        return null;
    }
}
