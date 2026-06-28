using System.Net.Mail;
using BespokeStudio.Application.Contracts.Orders;

namespace BespokeStudio.Application.Validation;

public static class OrderRequestValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, nameof(request.FullName), request.FullName, 200);
        AddRequired(errors, nameof(request.Description), request.Description, 4000);
        AddOptionalMaxLength(errors, nameof(request.Email), request.Email, 320);
        AddOptionalMaxLength(errors, nameof(request.Phone), request.Phone, 50);

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            errors[nameof(request.Email)] = ["Email or phone is required."];
            errors[nameof(request.Phone)] = ["Email or phone is required."];
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !MailAddress.TryCreate(request.Email.Trim(), out _))
        {
            errors[nameof(request.Email)] = ["Email has an invalid format."];
        }

        if (request.ServiceOfferingId.HasValue && !string.IsNullOrWhiteSpace(request.ServiceSlug))
        {
            errors[nameof(request.ServiceOfferingId)] =
                ["Provide either serviceOfferingId or serviceSlug, not both."];
        }
        else if (!request.ServiceOfferingId.HasValue && string.IsNullOrWhiteSpace(request.ServiceSlug))
        {
            if (!request.ServiceType.HasValue || !Enum.IsDefined(request.ServiceType.Value))
            {
                errors[nameof(request.ServiceType)] = ["A valid service selection is required."];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceSlug) && request.ServiceSlug.Trim().Length > 160)
        {
            errors[nameof(request.ServiceSlug)] = ["Service slug must not exceed 160 characters."];
        }

        if (!request.Consent)
        {
            errors[nameof(request.Consent)] = ["Consent is required to submit an enquiry."];
        }

        if (request.AttachmentIds is { Count: > 5 })
        {
            errors[nameof(request.AttachmentIds)] = ["No more than 5 attachments are allowed."];
        }
        else if (request.AttachmentIds is { Count: > 0 } &&
                 request.AttachmentIds.Distinct().Count() != request.AttachmentIds.Count)
        {
            errors[nameof(request.AttachmentIds)] = ["Attachment ids must be unique."];
        }

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(UpdateOrderStatusRequest request)
    {
        return Enum.IsDefined(request.Status)
            ? new Dictionary<string, string[]>()
            : new Dictionary<string, string[]>
            {
                [nameof(request.Status)] = ["Order status is invalid."]
            };
    }

    public static IReadOnlyDictionary<string, string[]> Validate(AddOrderNoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddRequired(errors, nameof(request.Text), request.Text, 2000);
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
}
