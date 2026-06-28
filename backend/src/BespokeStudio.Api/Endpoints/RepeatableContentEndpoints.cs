using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.RepeatableContent;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class RepeatableContentEndpoints
{
    public static IEndpointRouteBuilder MapRepeatableContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var pub = endpoints.MapGroup("/api/repeatable-content").WithTags("Repeatable Content");

        pub.MapGet(string.Empty, async (IRepeatableContentService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.GetPublicGroupsAsync(cancellationToken)))
            .AllowAnonymous()
            .WithName("GetPublicRepeatableContentGroups")
            .Produces<IReadOnlyList<PublicRepeatableContentGroupResponse>>();

        pub.MapGet("/groups/{groupKey}", async (string groupKey, IRepeatableContentService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.GetPublicGroupAsync(groupKey, cancellationToken)))
            .AllowAnonymous()
            .WithName("GetPublicRepeatableContentGroup")
            .Produces<PublicRepeatableContentGroupResponse>();

        var admin = endpoints.MapGroup("/api/admin/repeatable-content")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Repeatable Content");

        admin.MapGet(string.Empty, async (IRepeatableContentService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.GetAdminItemsAsync(cancellationToken)))
            .WithName("GetAdminRepeatableContent")
            .Produces<IReadOnlyList<AdminRepeatableContentItemResponse>>();

        admin.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAdminRepeatableContentById")
            .Produces<AdminRepeatableContentItemResponse>()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPost(string.Empty, CreateAsync)
            .WithName("CreateAdminRepeatableContent")
            .Produces<AdminRepeatableContentItemResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        admin.MapPatch("/{id:guid}", UpdateAsync)
            .WithName("UpdateAdminRepeatableContent")
            .Produces<AdminRepeatableContentItemResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", DeleteOrArchiveAsync)
            .WithName("ArchiveAdminRepeatableContent")
            .Produces<DeleteRepeatableContentItemResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetByIdAsync(Guid id, IRepeatableContentService service, CancellationToken cancellationToken)
    {
        var result = await service.GetAdminByIdAsync(id, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateAsync(
        SaveRepeatableContentItemRequest request,
        IRepeatableContentService service,
        CancellationToken cancellationToken)
    {
        var errors = RepeatableContentValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        try
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return TypedResults.Created($"/api/admin/repeatable-content/{result.Id}", result);
        }
        catch (RepeatableContentConflictException exception)
        {
            return Conflict(exception);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveRepeatableContentItemRequest request,
        IRepeatableContentService service,
        CancellationToken cancellationToken)
    {
        var errors = RepeatableContentValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        try
        {
            var result = await service.UpdateAsync(id, request, cancellationToken);
            return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
        }
        catch (RepeatableContentConflictException exception)
        {
            return Conflict(exception);
        }
    }

    private static async Task<IResult> DeleteOrArchiveAsync(
        Guid id,
        IRepeatableContentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteOrArchiveAsync(id, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static IResult Conflict(RepeatableContentConflictException exception) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [JsonNamingPolicy.CamelCase.ConvertName(exception.Field)] = [exception.Message]
        });

    private static Dictionary<string, string[]> ToJsonPropertyNames(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
