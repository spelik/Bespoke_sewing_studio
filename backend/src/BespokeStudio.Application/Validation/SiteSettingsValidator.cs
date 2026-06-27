using System.Net.Mail;
using System.Text.RegularExpressions;
using BespokeStudio.Application.Contracts.SiteSettings;

namespace BespokeStudio.Application.Validation;

public static partial class SiteSettingsValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(UpdateSiteSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, nameof(request.StudioName), request.StudioName, 150);
        AddOptionalMaxLength(errors, nameof(request.SiteTagline), request.SiteTagline, 500);
        AddEmail(errors, nameof(request.PublicEmail), request.PublicEmail);
        AddPhone(errors, nameof(request.PublicPhone), request.PublicPhone);
        AddPhone(errors, nameof(request.WhatsAppPhone), request.WhatsAppPhone);
        AddOptionalMaxLength(errors, nameof(request.ContactButtonLabel), request.ContactButtonLabel, 100);
        AddOptionalMaxLength(errors, nameof(request.ContactIntroText), request.ContactIntroText, 1000);
        AddEmail(errors, nameof(request.NotificationEmail), request.NotificationEmail);
        AddPhone(errors, nameof(request.NotificationPhone), request.NotificationPhone);
        AddUrl(errors, nameof(request.FacebookUrl), request.FacebookUrl);
        AddUrl(errors, nameof(request.InstagramUrl), request.InstagramUrl);
        AddUrl(errors, nameof(request.TikTokUrl), request.TikTokUrl);
        AddUrl(errors, nameof(request.PinterestUrl), request.PinterestUrl);
        AddOptionalMaxLength(errors, nameof(request.FooterText), request.FooterText, 500);
        AddOptionalMaxLength(errors, nameof(request.ServiceAreaText), request.ServiceAreaText, 500);
        AddOptionalMaxLength(errors, nameof(request.BusinessLegalName), request.BusinessLegalName, 200);

        if (request.EmailNotificationsEnabled && string.IsNullOrWhiteSpace(request.NotificationEmail))
        {
            errors[nameof(request.NotificationEmail)] =
                ["Notification email is required when email notifications are enabled."];
        }

        if (request.WhatsAppNotificationsEnabled && string.IsNullOrWhiteSpace(request.NotificationPhone))
        {
            errors[nameof(request.NotificationPhone)] =
                ["Notification phone is required when WhatsApp notifications are enabled."];
        }

        return errors;
    }

    private static void AddRequired(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required."];
        }
        else if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"{field} must not exceed {maxLength} characters."];
        }
    }

    private static void AddOptionalMaxLength(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            errors[field] = [$"{field} must not exceed {maxLength} characters."];
        }
    }

    private static void AddEmail(
        IDictionary<string, string[]> errors,
        string field,
        string? value)
    {
        AddOptionalMaxLength(errors, field, value, 320);
        if (!string.IsNullOrWhiteSpace(value) && !MailAddress.TryCreate(value.Trim(), out _))
        {
            errors[field] = [$"{field} has an invalid format."];
        }
    }

    private static void AddPhone(
        IDictionary<string, string[]> errors,
        string field,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 50)
        {
            errors[field] = [$"{field} must not exceed 50 characters."];
        }
        else if (!PhonePattern().IsMatch(trimmed) || trimmed.Count(char.IsDigit) < 5)
        {
            errors[field] = [$"{field} has an invalid format."];
        }
    }

    private static void AddUrl(
        IDictionary<string, string[]> errors,
        string field,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 2048 ||
            !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors[field] = [$"{field} must be a valid HTTP or HTTPS URL."];
        }
    }

    [GeneratedRegex("^[0-9+() .-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
