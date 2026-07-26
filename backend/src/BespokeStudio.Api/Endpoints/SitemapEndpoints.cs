using BespokeStudio.Application.Abstractions;

namespace BespokeStudio.Api.Endpoints;

public static class SitemapEndpoints
{
    public static IEndpointRouteBuilder MapSitemapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/sitemap.xml", GetSitemapAsync)
            .AllowAnonymous()
            .WithName("GetSitemap")
            .Produces(StatusCodes.Status200OK, contentType: "application/xml");

        return endpoints;
    }

    private static async Task<IResult> GetSitemapAsync(
        ISitemapService sitemapService,
        CancellationToken cancellationToken)
    {
        var xml = await sitemapService.BuildXmlAsync(cancellationToken);
        return Results.Content(xml, "application/xml; charset=utf-8");
    }
}
