using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Common;
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
            .Produces<PagedResponse<EmailDeliveryLogEntryResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        int? page,
        int? pageSize,
        string? search,
        string? messageType,
        string? status,
        string? recipientEmail,
        string? provider,
        IEmailDeliveryLogService service,
        CancellationToken cancellationToken)
    {
        var pagination = PaginationQuery.Normalize(page, pageSize);

        var entries = await service.GetAsync(
            new EmailDeliveryLogQueryRequest(
                pagination.Page,
                pagination.PageSize,
                search,
                messageType,
                status,
                recipientEmail,
                provider),
            cancellationToken);

        return TypedResults.Ok(entries);
    }
}
