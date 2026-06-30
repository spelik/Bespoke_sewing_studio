using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Security;

namespace BespokeStudio.Api.Endpoints;

public static class AdminAuditLogEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuditLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/audit-log")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Audit Log");

        admin.MapGet(string.Empty, GetAsync)
            .WithName("GetAdminAuditLog")
            .Produces<IReadOnlyList<AdminAuditLogEntryResponse>>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        int? take,
        string? search,
        string? action,
        string? entityType,
        string? actorEmail,
        IAdminAuditLogService service,
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
            new AdminAuditLogQueryRequest(limit, search, action, entityType, actorEmail),
            cancellationToken);

        return TypedResults.Ok(entries);
    }
}
