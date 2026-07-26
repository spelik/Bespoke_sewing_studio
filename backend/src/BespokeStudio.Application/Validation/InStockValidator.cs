using System.Text.RegularExpressions;
using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Validation;

public static partial class InStockValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(SaveInStockItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, nameof(request.Title), request.Title, 200);
        Optional(errors, nameof(request.Slug), request.Slug, 220);
        Optional(errors, nameof(request.ShortDescription), request.ShortDescription, 500);
        Optional(errors, nameof(request.Description), request.Description, 4000);
        Optional(errors, nameof(request.Sizes), request.Sizes, 500);
        Optional(errors, nameof(request.Materials), request.Materials, 1000);
        ValidateSlug(errors, nameof(request.Slug), request.Slug);
        NonNegative(errors, nameof(request.DisplayOrder), request.DisplayOrder);

        if (request.Price < 0m)
        {
            errors[nameof(request.Price)] = ["Price must be zero or greater."];
        }

        if (!Enum.IsDefined(request.Status))
        {
            errors[nameof(request.Status)] = ["Select a valid availability status."];
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? InStockItem.DefaultCurrency
            : request.Currency.Trim().ToUpperInvariant();
        if (!string.Equals(currency, InStockItem.DefaultCurrency, StringComparison.Ordinal))
        {
            errors[nameof(request.Currency)] = [$"Currency must be {InStockItem.DefaultCurrency}."];
        }

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(UpdateInStockImageRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Optional(errors, nameof(request.AltText), request.AltText, 250);
        NonNegative(errors, nameof(request.DisplayOrder), request.DisplayOrder);
        return errors;
    }

    /// <summary>
    /// Parses optional multipart displayOrder. Blank means omit; invalid integers are errors.
    /// </summary>
    public static bool TryParseOptionalDisplayOrder(
        string? rawValue,
        out int? displayOrder,
        out string? error)
    {
        displayOrder = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (!int.TryParse(rawValue.Trim(), out var parsed))
        {
            error = "Display order must be a valid integer.";
            return false;
        }

        if (parsed < 0)
        {
            error = "Display order must be zero or greater.";
            return false;
        }

        displayOrder = parsed;
        return true;
    }

    private static void Required(Dictionary<string, string[]> errors, string field, string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = ["This field is required."];
        }
        else if (value.Trim().Length > max)
        {
            errors[field] = [$"This field must not exceed {max} characters."];
        }
    }

    private static void Optional(Dictionary<string, string[]> errors, string field, string? value, int max)
    {
        if (value?.Trim().Length > max)
        {
            errors[field] = [$"This field must not exceed {max} characters."];
        }
    }

    private static void ValidateSlug(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !SlugPattern().IsMatch(value.Trim()))
        {
            errors[field] = ["Use a lowercase kebab-case slug."];
        }
    }

    private static void NonNegative(Dictionary<string, string[]> errors, string field, int value)
    {
        if (value < 0)
        {
            errors[field] = ["Display order must be zero or greater."];
        }
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
