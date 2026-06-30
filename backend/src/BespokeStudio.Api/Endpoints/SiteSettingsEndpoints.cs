using System.Security.Claims;
using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.SiteSettings;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class SiteSettingsEndpoints
{
    public static IEndpointRouteBuilder MapSiteSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/site-settings/public", GetPublicSettingsAsync)
            .AllowAnonymous()
            .WithTags("Site Settings")
            .WithName("GetPublicSiteSettings")
            .Produces<PublicSiteSettingsResponse>();

        var admin = endpoints.MapGroup("/api/admin/site-settings")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Site Settings");

        admin.MapGet(string.Empty, GetAdminSettingsAsync)
            .WithName("GetAdminSiteSettings")
            .Produces<AdminSiteSettingsResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPatch(string.Empty, UpdateSettingsAsync)
            .WithName("UpdateAdminSiteSettings")
            .Produces<AdminSiteSettingsResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetPublicSettingsAsync(
        ISiteSettingsService settingsService,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetPublicSettingsAsync(cancellationToken);
        return TypedResults.Ok(settings);
    }

    private static async Task<IResult> GetAdminSettingsAsync(
        ISiteSettingsService settingsService,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAdminSettingsAsync(cancellationToken);
        return TypedResults.Ok(settings);
    }

    private static async Task<IResult> UpdateSettingsAsync(
        UpdateSiteSettingsRequest request,
        ClaimsPrincipal principal,
        ISiteSettingsService settingsService,
        IAdminAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var errors = SiteSettingsValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var settings = await settingsService.UpdateSettingsAsync(request, cancellationToken);
        await auditLogService.RecordAsync(
            AdminAuditEndpointHelpers.CreateAuditRequest(
                principal,
                "site_settings.updated",
                "SiteSettings",
                settings.Id.ToString(),
                settings.StudioName,
                "Site settings were updated."),
            cancellationToken);
        return TypedResults.Ok(settings);
    }

    private static Dictionary<string, string[]> ToJsonPropertyNames(
        IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
