using BespokeStudio.Application.Contracts.SiteSettings;

namespace BespokeStudio.Application.Validation;

public static class BrandSettingsValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(UpdateBrandSettingsRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        Required(errors, nameof(request.BrandDisplayName), request.BrandDisplayName, 150);
        Required(errors, nameof(request.LogoAltText), request.LogoAltText, 200);
        Required(errors, nameof(request.HeaderCtaLabel), request.HeaderCtaLabel, 100);
        Required(errors, nameof(request.DefaultMetaTitle), request.DefaultMetaTitle, 200);
        Required(errors, nameof(request.DefaultMetaDescription), request.DefaultMetaDescription, 500);
        Optional(errors, nameof(request.DefaultOgTitle), request.DefaultOgTitle, 200);
        Optional(errors, nameof(request.DefaultOgDescription), request.DefaultOgDescription, 500);
        Required(errors, nameof(request.ServicesLabel), request.ServicesLabel, 50);
        Required(errors, nameof(request.PortfolioLabel), request.PortfolioLabel, 50);
        Required(errors, nameof(request.OrderLabel), request.OrderLabel, 50);
        Required(errors, nameof(request.AboutLabel), request.AboutLabel, 50);
        Required(errors, nameof(request.ContactLabel), request.ContactLabel, 50);
        if (!IsSafeUrl(request.HeaderCtaUrl))
            errors[nameof(request.HeaderCtaUrl)] = ["HeaderCtaUrl must be a relative URL or an HTTP/HTTPS URL."];
        return errors;
    }

    private static void Required(IDictionary<string,string[]> errors,string field,string? value,int max)
    { if(string.IsNullOrWhiteSpace(value)) errors[field]=[$"{field} is required."]; else if(value.Trim().Length>max) errors[field]=[$"{field} must not exceed {max} characters."]; }
    private static void Optional(IDictionary<string,string[]> errors,string field,string? value,int max)
    { if(!string.IsNullOrWhiteSpace(value)&&value.Trim().Length>max) errors[field]=[$"{field} must not exceed {max} characters."]; }
    private static bool IsSafeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 2048) return false;
        var url=value.Trim();
        if (url.StartsWith('/') && !url.StartsWith("//",StringComparison.Ordinal)) return true;
        return Uri.TryCreate(url,UriKind.Absolute,out var uri) && (uri.Scheme==Uri.UriSchemeHttp||uri.Scheme==Uri.UriSchemeHttps);
    }
}

public sealed class BrandSettingsConflictException(string field, string message) : Exception(message)
{ public string Field { get; } = field; }
