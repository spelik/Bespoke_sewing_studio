using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Services;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class ServiceOfferingEndpoints
{
    public static IEndpointRouteBuilder MapServiceOfferingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/services", GetPublicServicesAsync)
            .AllowAnonymous()
            .WithTags("Services")
            .WithName("GetPublicServices")
            .Produces<IReadOnlyList<PublicServiceOfferingResponse>>();

        var admin = endpoints.MapGroup("/api/admin/services")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Services");

        admin.MapGet(string.Empty, GetAdminServicesAsync)
            .WithName("GetAdminServices")
            .Produces<IReadOnlyList<AdminServiceOfferingResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAdminServiceById")
            .Produces<AdminServiceOfferingResponse>()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPost(string.Empty, CreateAsync)
            .WithName("CreateAdminService")
            .Produces<AdminServiceOfferingResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        admin.MapPatch("/{id:guid}", UpdateAsync)
            .WithName("UpdateAdminService")
            .Produces<AdminServiceOfferingResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", DeleteOrArchiveAsync)
            .WithName("DeleteOrArchiveAdminService")
            .Produces<DeleteServiceOfferingResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetPublicServicesAsync(
        IServiceOfferingService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetPublicServicesAsync(cancellationToken));

    private static async Task<IResult> GetAdminServicesAsync(
        IServiceOfferingService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAdminServicesAsync(cancellationToken));

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IServiceOfferingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateAsync(
        CreateServiceOfferingRequest request,
        IServiceOfferingService service,
        CancellationToken cancellationToken)
    {
        var errors = ServiceOfferingValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        try
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return TypedResults.Created($"/api/admin/services/{result.Id}", result);
        }
        catch (ServiceOfferingConflictException exception)
        {
            return Conflict(exception);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateServiceOfferingRequest request,
        IServiceOfferingService service,
        CancellationToken cancellationToken)
    {
        var errors = ServiceOfferingValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        try
        {
            var result = await service.UpdateAsync(id, request, cancellationToken);
            return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
        }
        catch (ServiceOfferingConflictException exception)
        {
            return Conflict(exception);
        }
    }

    private static async Task<IResult> DeleteOrArchiveAsync(
        Guid id,
        IServiceOfferingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteOrArchiveAsync(id, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static IResult Conflict(ServiceOfferingConflictException exception) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [JsonNamingPolicy.CamelCase.ConvertName(exception.Field)] = [exception.Message]
        });

    private static Dictionary<string, string[]> ToJsonPropertyNames(
        IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
