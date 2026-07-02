using System.Security.Claims;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Storage;
using BespokeStudio.Application.Security;

namespace BespokeStudio.Api.Endpoints;

public static class StorageMaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapStorageMaintenanceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var storage = endpoints.MapGroup("/api/admin/storage")
            .WithTags("Admin Storage")
            .RequireAuthorization(AdminAccess.PolicyName);

        storage.MapGet("/scan", ScanAsync)
            .WithName("ScanAdminStorage")
            .Produces<StorageScanResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        storage.MapPost("/delete-orphans", DeleteOrphansAsync)
            .WithName("DeleteAdminStorageOrphans")
            .Produces<StorageCleanupResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> ScanAsync(
        IStorageMaintenanceService storageMaintenanceService,
        CancellationToken cancellationToken)
    {
        var result = await storageMaintenanceService.ScanAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> DeleteOrphansAsync(
        ClaimsPrincipal principal,
        IStorageMaintenanceService storageMaintenanceService,
        CancellationToken cancellationToken)
    {
        var result = await storageMaintenanceService.DeleteOrphansAsync(
            AdminAuditEndpointHelpers.GetActor(principal),
            cancellationToken);
        return TypedResults.Ok(result);
    }
}
