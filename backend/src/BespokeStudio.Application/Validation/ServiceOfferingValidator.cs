using System.Text.RegularExpressions;
using BespokeStudio.Application.Contracts.Services;

namespace BespokeStudio.Application.Validation;

public static partial class ServiceOfferingValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(CreateServiceOfferingRequest request) =>
        ValidateCore(
            request.Slug,
            request.Name,
            request.ShortDescription,
            request.Description,
            request.Category,
            request.DisplayOrder,
            request.PriceOptions,
            request.ImageUrl);

    public static IReadOnlyDictionary<string, string[]> Validate(UpdateServiceOfferingRequest request) =>
        ValidateCore(
            request.Slug,
            request.Name,
            request.ShortDescription,
            request.Description,
            request.Category,
            request.DisplayOrder,
            request.PriceOptions,
            request.ImageUrl);

    private static IReadOnlyDictionary<string, string[]> ValidateCore(
        string? slug,
        string name,
        string shortDescription,
        string? description,
        string? category,
        int displayOrder,
        IReadOnlyCollection<ServicePriceOptionRequest>? priceOptions,
        string? imageUrl)
    {
        var errors = new Dictionary<string, string[]>();
        AddRequired(errors, "Name", name, 150);
        AddRequired(errors, "ShortDescription", shortDescription, 500);
        AddOptionalMaxLength(errors, "Description", description, 4000);
        AddOptionalMaxLength(errors, "Category", category, 100);

        if (!string.IsNullOrWhiteSpace(slug) &&
            (slug.Trim().Length > 160 || !SlugPattern().IsMatch(slug.Trim())))
        {
            errors["Slug"] =
                ["Slug must be lowercase kebab-case using letters, numbers and single hyphens."];
        }

        if (displayOrder < 0)
        {
            errors["DisplayOrder"] = ["Display order must be zero or greater."];
        }

        if (!string.IsNullOrWhiteSpace(imageUrl) &&
            (!Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            errors["ImageUrl"] = ["Image URL must be a valid HTTP or HTTPS URL."];
        }

        if (priceOptions is { Count: > 20 })
        {
            errors["PriceOptions"] = ["No more than 20 price options are allowed per service."];
        }
        else if (priceOptions is not null)
        {
            var optionErrors = new List<string>();
            var index = 0;
            foreach (var option in priceOptions)
            {
                if (string.IsNullOrWhiteSpace(option.Label))
                {
                    optionErrors.Add($"Price option {index + 1}: label is required.");
                }
                else if (option.Label.Trim().Length > 150)
                {
                    optionErrors.Add($"Price option {index + 1}: label must not exceed 150 characters.");
                }

                if (string.IsNullOrWhiteSpace(option.PriceText))
                {
                    optionErrors.Add($"Price option {index + 1}: price text is required.");
                }
                else if (option.PriceText.Trim().Length > 100)
                {
                    optionErrors.Add($"Price option {index + 1}: price text must not exceed 100 characters.");
                }

                if (!string.IsNullOrWhiteSpace(option.Description) && option.Description.Trim().Length > 500)
                {
                    optionErrors.Add($"Price option {index + 1}: description must not exceed 500 characters.");
                }

                if (option.DisplayOrder < 0)
                {
                    optionErrors.Add($"Price option {index + 1}: display order must be zero or greater.");
                }

                index++;
            }

            if (optionErrors.Count > 0)
            {
                errors["PriceOptions"] = optionErrors.ToArray();
            }
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

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
