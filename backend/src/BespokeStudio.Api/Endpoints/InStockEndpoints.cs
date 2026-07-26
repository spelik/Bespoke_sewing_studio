using System.Security.Claims;
using System.Text.Json;
using BespokeStudio.Api.Caching;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;
using Microsoft.AspNetCore.OutputCaching;

namespace BespokeStudio.Api.Endpoints;

public static class InStockEndpoints
{
    public static IEndpointRouteBuilder MapInStockEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/in-stock").WithTags("IN STOCK");
        publicGroup.MapGet(string.Empty, async (IInStockService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetPublicItemsAsync(ct)))
            .AllowAnonymous()
            .CachePublicContent(PublicOutputCachePolicy.InStockTag)
            .WithName("GetPublicInStockItems")
            .Produces<IReadOnlyList<PublicInStockItemResponse>>();

        // Register before /{slug} so "images" is not captured as a slug.
        publicGroup.MapGet("/images/{imageId:guid}", OpenPublicImageAsync)
            .AllowAnonymous()
            .WithName("GetPublicInStockImage")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        publicGroup.MapGet("/{slug}", GetPublicBySlugAsync)
            .AllowAnonymous()
            .CachePublicContent(PublicOutputCachePolicy.InStockTag)
            .WithName("GetPublicInStockItemBySlug")
            .Produces<PublicInStockItemResponse>()
            .Produces(StatusCodes.Status404NotFound);

        var admin = endpoints.MapGroup("/api/admin/in-stock")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin IN STOCK");

        admin.MapGet("/items", async (IInStockService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetAdminItemsAsync(ct)))
            .WithName("GetAdminInStockItems")
            .Produces<IReadOnlyList<AdminInStockItemResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapGet("/items/{id:guid}", GetAdminItemAsync)
            .WithName("GetAdminInStockItem")
            .Produces<AdminInStockItemResponse>()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPost("/items", CreateItemAsync)
            .WithName("CreateAdminInStockItem")
            .Produces<AdminInStockItemResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        admin.MapPut("/items/{id:guid}", UpdateItemAsync)
            .WithName("UpdateAdminInStockItem")
            .Produces<AdminInStockItemResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPost("/items/{id:guid}/archive", ArchiveItemAsync)
            .WithName("ArchiveAdminInStockItem")
            .Produces<ArchiveInStockItemResponse>()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPost("/items/{id:guid}/restore", RestoreItemAsync)
            .WithName("RestoreAdminInStockItem")
            .Produces<ArchiveInStockItemResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        admin.MapPost("/items/{id:guid}/images", AddImageAsync)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .WithName("UploadAdminInStockImage")
            .Produces<AdminInStockImageResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPatch("/items/{id:guid}/images/{imageId:guid}", UpdateImageAsync)
            .WithName("UpdateAdminInStockImage")
            .Produces<AdminInStockImageResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        admin.MapDelete("/items/{id:guid}/images/{imageId:guid}", DeleteImageAsync)
            .WithName("DeleteAdminInStockImage")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        admin.MapGet("/images/{imageId:guid}", OpenAdminImageAsync)
            .WithName("GetAdminInStockImage")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetPublicBySlugAsync(
        string slug,
        IInStockService service,
        CancellationToken ct)
    {
        var item = await service.GetPublicItemBySlugAsync(slug, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    private static async Task<IResult> GetAdminItemAsync(
        Guid id,
        IInStockService service,
        CancellationToken ct)
    {
        var item = await service.GetAdminItemByIdAsync(id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    private static async Task<IResult> CreateItemAsync(
        SaveInStockItemRequest request,
        ClaimsPrincipal principal,
        IInStockService service,
        IAdminAuditLogService auditLogService,
        IOutputCacheStore outputCacheStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var errors = InStockValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Validation(errors);
        }

        try
        {
            var item = await service.CreateItemAsync(request, ct);
            await TrySecondaryAsync(
                loggerFactory,
                "create",
                () => auditLogService.RecordAsync(
                    AdminAuditEndpointHelpers.CreateAuditRequest(
                        principal,
                        "in_stock.created",
                        "InStockItem",
                        item.Id.ToString(),
                        item.Title,
                        "IN STOCK item was created."),
                    ct),
                () => EvictCacheAsync(outputCacheStore, ct));
            return TypedResults.Created($"/api/admin/in-stock/items/{item.Id}", item);
        }
        catch (InStockConflictException exception)
        {
            return Conflict(exception);
        }
    }

    private static async Task<IResult> UpdateItemAsync(
        Guid id,
        SaveInStockItemRequest request,
        ClaimsPrincipal principal,
        IInStockService service,
        IAdminAuditLogService auditLogService,
        IOutputCacheStore outputCacheStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var errors = InStockValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Validation(errors);
        }

        try
        {
            var item = await service.UpdateItemAsync(id, request, ct);
            if (item is null)
            {
                return TypedResults.NotFound();
            }

            await TrySecondaryAsync(
                loggerFactory,
                "update",
                () => auditLogService.RecordAsync(
                    AdminAuditEndpointHelpers.CreateAuditRequest(
                        principal,
                        "in_stock.updated",
                        "InStockItem",
                        item.Id.ToString(),
                        item.Title,
                        "IN STOCK item was updated."),
                    ct),
                () => EvictCacheAsync(outputCacheStore, ct));
            return TypedResults.Ok(item);
        }
        catch (InStockConflictException exception)
        {
            return Conflict(exception);
        }
    }

    private static async Task<IResult> ArchiveItemAsync(
        Guid id,
        ClaimsPrincipal principal,
        IInStockService service,
        IAdminAuditLogService auditLogService,
        IOutputCacheStore outputCacheStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var result = await service.ArchiveItemAsync(id, ct);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        await TrySecondaryAsync(
            loggerFactory,
            "archive",
            () => auditLogService.RecordAsync(
                AdminAuditEndpointHelpers.CreateAuditRequest(
                    principal,
                    "in_stock.archived",
                    "InStockItem",
                    id.ToString(),
                    null,
                    result.Message),
                ct),
            () => EvictCacheAsync(outputCacheStore, ct));
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RestoreItemAsync(
        Guid id,
        ClaimsPrincipal principal,
        IInStockService service,
        IAdminAuditLogService auditLogService,
        IOutputCacheStore outputCacheStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        try
        {
            var result = await service.RestoreItemAsync(id, ct);
            if (result is null)
            {
                return TypedResults.NotFound();
            }

            if (result.Restored)
            {
                await TrySecondaryAsync(
                    loggerFactory,
                    "restore",
                    () => auditLogService.RecordAsync(
                        AdminAuditEndpointHelpers.CreateAuditRequest(
                            principal,
                            "in_stock.restored",
                            "InStockItem",
                            id.ToString(),
                            null,
                            result.Message),
                        ct),
                    () => EvictCacheAsync(outputCacheStore, ct));
            }

            return TypedResults.Ok(result);
        }
        catch (InStockConflictException exception)
        {
            return Conflict(exception);
        }
    }

    private static async Task<IResult> AddImageAsync(
        Guid id,
        HttpRequest request,
        ClaimsPrincipal principal,
        IInStockService service,
        IAdminAuditLogService auditLogService,
        IOutputCacheStore outputCacheStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!request.HasFormContentType)
        {
            return FieldProblem("file", "A multipart/form-data request is required.");
        }

        try
        {
            var form = await request.ReadFormAsync(ct);
            if (form.Files.Count != 1)
            {
                return FieldProblem("file", "Select exactly one IN STOCK image.");
            }

            var file = form.Files[0];
            var altText = form.TryGetValue("altText", out var altValues) ? altValues.ToString() : null;

            string? displayOrderRaw = null;
            if (form.TryGetValue("displayOrder", out var orderValues))
            {
                displayOrderRaw = orderValues.ToString();
            }

            if (!InStockValidator.TryParseOptionalDisplayOrder(displayOrderRaw, out var displayOrder, out var displayOrderError))
            {
                return FieldProblem("displayOrder", displayOrderError!);
            }

            await using var stream = file.OpenReadStream();
            var image = await service.AddImageAsync(
                id,
                new UploadFileRequest(file.FileName, file.ContentType, file.Length, stream),
                altText,
                displayOrder,
                ct);
            if (image is null)
            {
                return TypedResults.NotFound();
            }

            await TrySecondaryAsync(
                loggerFactory,
                "image_upload",
                () => auditLogService.RecordAsync(
                    AdminAuditEndpointHelpers.CreateAuditRequest(
                        principal,
                        "in_stock.image_uploaded",
                        "InStockItemImage",
                        image.Id.ToString(),
                        image.OriginalFileName,
                        "IN STOCK image was uploaded."),
                    ct),
                () => EvictCacheAsync(outputCacheStore, ct));
            return TypedResults.Ok(image);
        }
        catch (InvalidDataException)
        {
            return FieldProblem("file", "The upload request exceeds the configured size limit.");
        }
        catch (UploadValidationException exception)
        {
            return FieldProblem("file", exception.Message);
        }
    }

    private static async Task<IResult> UpdateImageAsync(
        Guid id,
        Guid imageId,
        UpdateInStockImageRequest request,
        ClaimsPrincipal principal,
        IInStockService service,
        IAdminAuditLogService auditLogService,
        IOutputCacheStore outputCacheStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var errors = InStockValidator.Validate(request);
        if (errors.Count > 0)
        {
            return Validation(errors);
        }

        var image = await service.UpdateImageAsync(id, imageId, request, ct);
        if (image is null)
        {
            return TypedResults.NotFound();
        }

        await TrySecondaryAsync(
            loggerFactory,
            "image_update",
            () => auditLogService.RecordAsync(
                AdminAuditEndpointHelpers.CreateAuditRequest(
                    principal,
                    "in_stock.image_updated",
                    "InStockItemImage",
                    image.Id.ToString(),
                    image.OriginalFileName,
                    "IN STOCK image metadata was updated."),
                ct),
            () => EvictCacheAsync(outputCacheStore, ct));
        return TypedResults.Ok(image);
    }

    private static async Task<IResult> DeleteImageAsync(
        Guid id,
        Guid imageId,
        ClaimsPrincipal principal,
        IInStockService service,
        IAdminAuditLogService auditLogService,
        IOutputCacheStore outputCacheStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var deleted = await service.DeleteImageAsync(id, imageId, ct);
        if (!deleted)
        {
            return TypedResults.NotFound();
        }

        await TrySecondaryAsync(
            loggerFactory,
            "image_delete",
            () => auditLogService.RecordAsync(
                AdminAuditEndpointHelpers.CreateAuditRequest(
                    principal,
                    "in_stock.image_deleted",
                    "InStockItemImage",
                    imageId.ToString(),
                    null,
                    "IN STOCK image was deleted and physical cleanup was scheduled."),
                ct),
            () => EvictCacheAsync(outputCacheStore, ct));
        return TypedResults.NoContent();
    }

    private static async Task<IResult> OpenPublicImageAsync(
        Guid imageId,
        IUploadService service,
        CancellationToken ct)
    {
        var file = await service.OpenPublicInStockImageAsync(imageId, ct);
        return file is null
            ? TypedResults.NotFound()
            : Results.File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    private static async Task<IResult> OpenAdminImageAsync(
        Guid imageId,
        IUploadService service,
        CancellationToken ct)
    {
        var file = await service.OpenInStockImageForAdminAsync(imageId, ct);
        return file is null
            ? TypedResults.NotFound()
            : Results.File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    private static async Task TrySecondaryAsync(
        ILoggerFactory loggerFactory,
        string operation,
        Func<Task> audit,
        Func<ValueTask> cacheEvict)
    {
        var logger = loggerFactory.CreateLogger("BespokeStudio.Api.Endpoints.InStock");

        try
        {
            await audit();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "IN STOCK audit logging failed after a successful {Operation}. Primary mutation was kept.",
                operation);
        }

        try
        {
            await cacheEvict();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "IN STOCK output-cache invalidation failed after a successful {Operation}. Primary mutation was kept.",
                operation);
        }
    }

    private static ValueTask EvictCacheAsync(
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken) =>
        PublicOutputCacheInvalidation.EvictAsync(
            outputCacheStore,
            cancellationToken,
            PublicOutputCachePolicy.InStockTag);

    private static IResult Validation(IReadOnlyDictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(
            errors.ToDictionary(
                pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
                pair => pair.Value));

    private static IResult Conflict(InStockConflictException exception) =>
        FieldProblem(exception.Field, exception.Message);

    private static IResult FieldProblem(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [JsonNamingPolicy.CamelCase.ConvertName(field)] = [message]
        });
}
