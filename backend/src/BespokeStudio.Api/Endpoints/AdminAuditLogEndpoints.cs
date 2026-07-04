using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Common;
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
            .Produces<PagedResponse<AdminAuditLogEntryResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        int? page,
        int? pageSize,
        string? search,
        string? action,
        string? entityType,
        string? actorEmail,
        IAdminAuditLogService service,
        CancellationToken cancellationToken)
    {
        var pagination = PaginationQuery.Normalize(page, pageSize);

        var entries = await service.GetAsync(
            new AdminAuditLogQueryRequest(
                pagination.Page,
                pagination.PageSize,
                search,
                action,
                entityType,
                actorEmail),
            cancellationToken);

        return TypedResults.Ok(entries);
    }
}
