using System.Text.RegularExpressions;
using BespokeStudio.Application.Contracts.RepeatableContent;

namespace BespokeStudio.Application.Validation;

public static partial class RepeatableContentValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(SaveRepeatableContentItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        Key(errors, "GroupKey", request.GroupKey, 80);
        Key(errors, "ItemKey", request.ItemKey, 100);
        Max(errors, "Title", request.Title, 500);
        Max(errors, "Subtitle", request.Subtitle, 1000);
        Max(errors, "Body", request.Body, 12000);
        Max(errors, "Label", request.Label, 100);
        Max(errors, "Value", request.Value, 500);
        Max(errors, "IconKey", request.IconKey, 50);
        Max(errors, "Url", request.Url, 2048);
        Max(errors, "Location", request.Location, 250);
        Max(errors, "Service", request.Service, 250);

        if (request.DisplayOrder < 0)
        {
            errors["DisplayOrder"] = ["Display order must be zero or greater."];
        }

        if (request.Rating is < 1 or > 5)
        {
            errors["Rating"] = ["Rating must be between 1 and 5."];
        }

        if (!string.IsNullOrWhiteSpace(request.IconKey) && !SafeOptionalKeyPattern().IsMatch(request.IconKey))
        {
            errors["IconKey"] = ["Use a lowercase safe key with letters, numbers and hyphens."];
        }

        if (!string.IsNullOrWhiteSpace(request.Url) &&
            !Uri.TryCreate(request.Url, UriKind.Relative, out _) &&
            !(Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) &&
              (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
        {
            errors["Url"] = ["URL must be relative or use http/https."];
        }

        return errors;
    }

    private static void Key(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = ["This field is required."];
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength || !RequiredKeyPattern().IsMatch(trimmed))
        {
            errors[field] = ["Use a lowercase safe key with letters, numbers and hyphens."];
        }
    }

    private static void Max(Dictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        if (value?.Trim().Length > maxLength)
        {
            errors[field] = [$"This field must not exceed {maxLength} characters."];
        }
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex RequiredKeyPattern();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SafeOptionalKeyPattern();
}
