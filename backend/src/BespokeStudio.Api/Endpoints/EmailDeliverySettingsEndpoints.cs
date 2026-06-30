using System.Security.Claims;
using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class EmailDeliverySettingsEndpoints
{
    public static IEndpointRouteBuilder MapEmailDeliverySettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/email-delivery")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Email Delivery");

        admin.MapGet(string.Empty, GetSettingsAsync)
            .WithName("GetAdminEmailDeliverySettings")
            .Produces<AdminEmailDeliverySettingsResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPatch(string.Empty, UpdateSettingsAsync)
            .WithName("UpdateAdminEmailDeliverySettings")
            .Produces<AdminEmailDeliverySettingsResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetSettingsAsync(
        IEmailDeliverySettingsService settingsService,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAdminSettingsAsync(cancellationToken);
        return TypedResults.Ok(settings);
    }

    private static async Task<IResult> UpdateSettingsAsync(
        UpdateEmailDeliverySettingsRequest request,
        ClaimsPrincipal principal,
        IEmailDeliverySettingsService settingsService,
        IAdminAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await settingsService.UpdateAdminSettingsAsync(request, cancellationToken);
            await auditLogService.RecordAsync(
                AdminAuditEndpointHelpers.CreateAuditRequest(
                    principal,
                    "email_delivery.updated",
                    "EmailDeliverySettings",
                    null,
                    settings.Provider,
                    "Email delivery settings were updated."),
                cancellationToken);
            return TypedResults.Ok(settings);
        }
        catch (EmailDeliverySettingsValidationException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static Dictionary<string, string[]> ToJsonPropertyNames(
        IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
