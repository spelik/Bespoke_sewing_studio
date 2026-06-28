using System.Text.RegularExpressions;
using BespokeStudio.Application.Contracts.Portfolio;

namespace BespokeStudio.Application.Validation;

public static partial class PortfolioValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(SavePortfolioCategoryRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, nameof(request.Name), request.Name, 100);
        Optional(errors, nameof(request.Slug), request.Slug, 120);
        Optional(errors, nameof(request.Description), request.Description, 1000);
        ValidateSlug(errors, nameof(request.Slug), request.Slug);
        NonNegative(errors, nameof(request.DisplayOrder), request.DisplayOrder);
        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(SavePortfolioItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.CategoryId == Guid.Empty) errors[nameof(request.CategoryId)] = ["Select a valid category."];
        Required(errors, nameof(request.Title), request.Title, 200);
        Optional(errors, nameof(request.Slug), request.Slug, 220);
        Optional(errors, nameof(request.ShortDescription), request.ShortDescription, 500);
        Optional(errors, nameof(request.Description), request.Description, 4000);
        Optional(errors, nameof(request.AltText), request.AltText, 250);
        ValidateSlug(errors, nameof(request.Slug), request.Slug);
        NonNegative(errors, nameof(request.DisplayOrder), request.DisplayOrder);
        if (request.IsActive && request.ImageFileId is null)
            errors[nameof(request.ImageFileId)] = ["An image is required before the portfolio item can be active."];
        return errors;
    }

    private static void Required(Dictionary<string, string[]> errors, string field, string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[field] = ["This field is required."];
        else if (value.Trim().Length > max) errors[field] = [$"This field must not exceed {max} characters."];
    }

    private static void Optional(Dictionary<string, string[]> errors, string field, string? value, int max)
    {
        if (value?.Trim().Length > max) errors[field] = [$"This field must not exceed {max} characters."];
    }

    private static void ValidateSlug(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !SlugPattern().IsMatch(value.Trim()))
            errors[field] = ["Use a lowercase kebab-case slug."];
    }

    private static void NonNegative(Dictionary<string, string[]> errors, string field, int value)
    {
        if (value < 0) errors[field] = ["Display order must be zero or greater."];
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
