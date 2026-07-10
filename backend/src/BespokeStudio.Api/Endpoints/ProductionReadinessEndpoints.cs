using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.ProductionReadiness;
using BespokeStudio.Application.Security;

namespace BespokeStudio.Api.Endpoints;

public static class ProductionReadinessEndpoints
{
    public static IEndpointRouteBuilder MapProductionReadinessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/production-readiness")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Production Readiness");

        admin.MapGet(string.Empty, GetSummaryAsync)
            .WithName("GetAdminProductionReadiness")
            .Produces<ProductionReadinessResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
        IProductionReadinessService readinessService,
        CancellationToken cancellationToken)
    {
        var summary = await readinessService.GetSummaryAsync(cancellationToken);
        return TypedResults.Ok(summary);
    }
}
