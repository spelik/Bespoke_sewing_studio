using System.Net.Mail;
using BespokeStudio.Application.Contracts.Notifications;

namespace BespokeStudio.Application.Validation;

public static class EmailDeliverySettingsValidator
{
    public const string ConfigurationProvider = "Configuration";
    public const string GmailSmtpProvider = "GmailSmtp";
    public const string ResendApiProvider = "ResendApi";

    public static Dictionary<string, string[]> Validate(UpdateEmailDeliverySettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var provider = NormalizeProvider(request.Provider);

        if (provider is null)
        {
            errors[nameof(request.Provider)] =
                ["Choose Configuration, Gmail SMTP or Resend API as the email delivery provider."];
            return errors;
        }

        var senderName = Normalize(request.SenderName);
        if (!string.IsNullOrWhiteSpace(senderName) && senderName.Length > 150)
        {
            errors[nameof(request.SenderName)] = ["Sender name must be 150 characters or fewer."];
        }

        if ((provider == GmailSmtpProvider || provider == ResendApiProvider) &&
            string.IsNullOrWhiteSpace(senderName))
        {
            errors[nameof(request.SenderName)] = ["Sender name is required for the selected email provider."];
        }

        if (provider == GmailSmtpProvider)
        {
            var gmailAddress = NormalizeEmail(request.GmailAddress);
            if (string.IsNullOrWhiteSpace(gmailAddress) ||
                !MailAddress.TryCreate(gmailAddress, out _))
            {
                errors[nameof(request.GmailAddress)] = ["A valid Gmail address is required."];
            }
            else if (gmailAddress.Length > 320)
            {
                errors[nameof(request.GmailAddress)] = ["Gmail address must be 320 characters or fewer."];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.AppPassword))
        {
            var appPassword = NormalizeAppPassword(request.AppPassword);
            if (appPassword.Length < 16 || appPassword.Length > 128)
            {
                errors[nameof(request.AppPassword)] =
                    ["Enter a valid Google App Password. It should be at least 16 characters."];
            }
        }

        if (provider == ResendApiProvider)
        {
            var fromEmail = NormalizeEmail(request.ResendFromEmail);
            if (string.IsNullOrWhiteSpace(fromEmail) ||
                !MailAddress.TryCreate(fromEmail, out _))
            {
                errors[nameof(request.ResendFromEmail)] = ["A valid Resend From email is required."];
            }
            else if (fromEmail.Length > 320)
            {
                errors[nameof(request.ResendFromEmail)] = ["Resend From email must be 320 characters or fewer."];
            }

            var replyToEmail = NormalizeEmail(request.ReplyToEmail);
            if (string.IsNullOrWhiteSpace(replyToEmail) ||
                !MailAddress.TryCreate(replyToEmail, out _))
            {
                errors[nameof(request.ReplyToEmail)] = ["A valid Reply-To email is required."];
            }
            else if (replyToEmail.Length > 320)
            {
                errors[nameof(request.ReplyToEmail)] = ["Reply-To email must be 320 characters or fewer."];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ResendApiKey))
        {
            var apiKey = NormalizeSecret(request.ResendApiKey);
            if (apiKey.Length < 10 || apiKey.Length > 512)
            {
                errors[nameof(request.ResendApiKey)] = ["Enter a valid Resend API key."];
            }
        }

        return errors;
    }

    public static string? NormalizeProvider(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Equals(ConfigurationProvider, StringComparison.OrdinalIgnoreCase))
        {
            return ConfigurationProvider;
        }

        if (normalized.Equals(GmailSmtpProvider, StringComparison.OrdinalIgnoreCase))
        {
            return GmailSmtpProvider;
        }

        if (normalized.Equals(ResendApiProvider, StringComparison.OrdinalIgnoreCase))
        {
            return ResendApiProvider;
        }

        return null;
    }

    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? NormalizeEmail(string? value) =>
        Normalize(value)?.ToLowerInvariant();

    public static string NormalizeAppPassword(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

    public static string NormalizeSecret(string value) => value.Trim();
}
