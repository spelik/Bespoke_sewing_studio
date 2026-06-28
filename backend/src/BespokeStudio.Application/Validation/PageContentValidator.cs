using System.Text.RegularExpressions;
using BespokeStudio.Application.Contracts.Content;
namespace BespokeStudio.Application.Validation;
public static partial class PageContentValidator
{
    public static IReadOnlyDictionary<string,string[]> Validate(SavePageContentRequest request)
    {
        var e=new Dictionary<string,string[]>(); Key(e,"PageKey",request.PageKey,50); Key(e,"SectionKey",request.SectionKey,80);
        Max(e,"Title",request.Title,500); Max(e,"Subtitle",request.Subtitle,1000); Max(e,"Body",request.Body,12000);
        Max(e,"CtaLabel",request.CtaLabel,150); Max(e,"CtaUrl",request.CtaUrl,2048); Max(e,"ImageAltText",request.ImageAltText,250);
        if(request.DisplayOrder<0)e["DisplayOrder"]=["Display order must be zero or greater."];
        if(!string.IsNullOrWhiteSpace(request.CtaUrl) && !Uri.TryCreate(request.CtaUrl,UriKind.Relative,out _) &&
           !(Uri.TryCreate(request.CtaUrl,UriKind.Absolute,out var uri) && (uri.Scheme==Uri.UriSchemeHttp||uri.Scheme==Uri.UriSchemeHttps)))
            e["CtaUrl"]=["CTA URL must be relative or use http/https."];
        return e;
    }
    private static void Key(Dictionary<string,string[]> e,string f,string? v,int m){if(string.IsNullOrWhiteSpace(v))e[f]=["This field is required."];else if(v.Length>m||!KeyPattern().IsMatch(v))e[f]=["Use a lowercase safe key with letters, numbers and hyphens."];}
    private static void Max(Dictionary<string,string[]> e,string f,string? v,int m){if(v?.Trim().Length>m)e[f]=[$"This field must not exceed {m} characters."];}
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")] private static partial Regex KeyPattern();
}
