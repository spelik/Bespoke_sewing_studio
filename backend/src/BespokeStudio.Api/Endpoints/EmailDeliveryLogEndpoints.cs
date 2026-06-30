using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.EmailDeliveryLog;
using BespokeStudio.Application.Security;

namespace BespokeStudio.Api.Endpoints;

public static class EmailDeliveryLogEndpoints
{
    public static IEndpointRouteBuilder MapEmailDeliveryLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/email-log")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Email Log");

        admin.MapGet(string.Empty, GetAsync)
            .WithName("GetAdminEmailDeliveryLog")
            .Produces<IReadOnlyList<EmailDeliveryLogEntryResponse>>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        int? take,
        string? search,
        string? messageType,
        string? status,
        string? recipientEmail,
        string? provider,
        IEmailDeliveryLogService service,
        CancellationToken cancellationToken)
    {
        var limit = take ?? 100;
        if (limit is < 1 or > 200)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["take"] = ["Take must be between 1 and 200."]
            });
        }

        var entries = await service.GetAsync(
            new EmailDeliveryLogQueryRequest(limit, search, messageType, status, recipientEmail, provider),
            cancellationToken);

        return TypedResults.Ok(entries);
    }
}
