using System.Net.Mail;
using BespokeStudio.Application.Contracts.ContactMessages;

namespace BespokeStudio.Application.Validation;

public static class ContactMessageValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(CreateContactMessageRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        PublicFormSpamValidator.AddValidationErrors(errors, request.WebsiteUrl, request.FormLoadedAt);

        AddRequired(errors, nameof(request.FullName), request.FullName, 200);
        AddRequired(errors, nameof(request.Email), request.Email, 320);
        AddRequired(errors, nameof(request.Message), request.Message, 4000);
        AddOptionalMaxLength(errors, nameof(request.Phone), request.Phone, 50);
        AddOptionalMaxLength(errors, nameof(request.Subject), request.Subject, 250);

        if (!string.IsNullOrWhiteSpace(request.Email) && !MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            errors[nameof(request.Email)] = ["Email has an invalid format."];
        }

        if (!request.Consent)
        {
            errors[nameof(request.Consent)] = ["Consent is required to submit a contact message."];
        }

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(UpdateContactMessageStatusRequest request)
    {
        return Enum.IsDefined(request.Status)
            ? new Dictionary<string, string[]>()
            : new Dictionary<string, string[]>
            {
                [nameof(request.Status)] = ["Contact message status is invalid."]
            };
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
}
