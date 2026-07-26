using System.Net;
using System.Text;
using BespokeStudio.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace BespokeStudio.Infrastructure.Services;

public sealed class SitemapService(
    IInStockService inStockService,
    IConfiguration configuration) : ISitemapService
{
    private const string DefaultOrigin = "https://oksanalogosha.com";

    private static readonly (string Path, string ChangeFreq, string Priority)[] StaticEntries =
    [
        ("/", "monthly", "1.0"),
        ("/services", "monthly", "0.9"),
        ("/in-stock", "weekly", "0.85"),
        ("/portfolio", "monthly", "0.8"),
        ("/order", "monthly", "0.8"),
        ("/about", "monthly", "0.7"),
        ("/contact", "monthly", "0.7"),
        ("/privacy", "yearly", "0.4"),
        ("/terms", "yearly", "0.4"),
    ];

    public async Task<string> BuildXmlAsync(CancellationToken cancellationToken = default)
    {
        var origin = ResolveOrigin();
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        builder.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var entry in StaticEntries)
        {
            AppendUrl(builder, origin, entry.Path, entry.ChangeFreq, entry.Priority);
        }

        var items = await inStockService.GetPublicItemsAsync(cancellationToken);
        foreach (var item in items)
        {
            AppendUrl(
                builder,
                origin,
                $"/in-stock/{item.Slug}",
                "weekly",
                "0.75");
        }

        builder.AppendLine("</urlset>");
        return builder.ToString();
    }

    private string ResolveOrigin()
    {
        var configured = configuration["PublicSiteUrl"]
            ?? configuration["PUBLIC_SITE_URL"]
            ?? DefaultOrigin;
        return configured.Trim().TrimEnd('/');
    }

    private static void AppendUrl(
        StringBuilder builder,
        string origin,
        string path,
        string changeFreq,
        string priority)
    {
        var loc = $"{origin}{(path.StartsWith('/') ? path : "/" + path)}";
        builder.AppendLine("  <url>");
        builder.Append("    <loc>");
        builder.Append(WebUtility.HtmlEncode(loc));
        builder.AppendLine("</loc>");
        builder.Append("    <changefreq>");
        builder.Append(changeFreq);
        builder.AppendLine("</changefreq>");
        builder.Append("    <priority>");
        builder.Append(priority);
        builder.AppendLine("</priority>");
        builder.AppendLine("  </url>");
    }
}
