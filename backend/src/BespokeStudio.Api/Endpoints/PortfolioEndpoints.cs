using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Portfolio;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/portfolio").WithTags("Portfolio");
        publicGroup.MapGet(string.Empty, async (IPortfolioService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetPublicPortfolioAsync(ct)))
            .AllowAnonymous().WithName("GetPublicPortfolio").Produces<IReadOnlyList<PublicPortfolioItemResponse>>();
        publicGroup.MapGet("/categories", async (IPortfolioService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetPublicCategoriesAsync(ct)))
            .AllowAnonymous().WithName("GetPublicPortfolioCategories").Produces<IReadOnlyList<PublicPortfolioCategoryResponse>>();
        publicGroup.MapGet("/images/{id:guid}", OpenPublicImageAsync)
            .AllowAnonymous().WithName("GetPublicPortfolioImage").Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

        var admin = endpoints.MapGroup("/api/admin/portfolio")
            .RequireAuthorization(AdminAccess.PolicyName).WithTags("Admin Portfolio");
        admin.MapGet("/items", async (IPortfolioService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetAdminItemsAsync(ct))).WithName("GetAdminPortfolioItems");
        admin.MapGet("/items/{id:guid}", GetItemAsync).WithName("GetAdminPortfolioItem");
        admin.MapPost("/items", CreateItemAsync).WithName("CreateAdminPortfolioItem").ProducesValidationProblem();
        admin.MapPatch("/items/{id:guid}", UpdateItemAsync).WithName("UpdateAdminPortfolioItem").ProducesValidationProblem();
        admin.MapDelete("/items/{id:guid}", DeleteItemAsync).WithName("DeleteAdminPortfolioItem");
        admin.MapGet("/categories", async (IPortfolioService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetAdminCategoriesAsync(ct))).WithName("GetAdminPortfolioCategories");
        admin.MapPost("/categories", CreateCategoryAsync).WithName("CreateAdminPortfolioCategory").ProducesValidationProblem();
        admin.MapPatch("/categories/{id:guid}", UpdateCategoryAsync).WithName("UpdateAdminPortfolioCategory").ProducesValidationProblem();
        admin.MapDelete("/categories/{id:guid}", DeleteCategoryAsync).WithName("DeleteAdminPortfolioCategory");
        admin.MapPost("/uploads", UploadImageAsync).DisableAntiforgery().Accepts<IFormFile>("multipart/form-data")
            .WithName("UploadAdminPortfolioImage").Produces<UploadedFileResponse>().ProducesValidationProblem();
        admin.MapGet("/images/{id:guid}", OpenAdminImageAsync)
            .WithName("GetAdminPortfolioImage").Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static async Task<IResult> GetItemAsync(Guid id, IPortfolioService service, CancellationToken ct)
    {
        var item = await service.GetAdminItemByIdAsync(id, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    private static async Task<IResult> CreateItemAsync(SavePortfolioItemRequest request, IPortfolioService service, CancellationToken ct)
    {
        var errors = PortfolioValidator.Validate(request);
        if (errors.Count > 0) return Validation(errors);
        try
        {
            var item = await service.CreateItemAsync(request, ct);
            return TypedResults.Created($"/api/admin/portfolio/items/{item.Id}", item);
        }
        catch (PortfolioConflictException exception) { return Conflict(exception); }
    }

    private static async Task<IResult> UpdateItemAsync(Guid id, SavePortfolioItemRequest request, IPortfolioService service, CancellationToken ct)
    {
        var errors = PortfolioValidator.Validate(request);
        if (errors.Count > 0) return Validation(errors);
        try
        {
            var item = await service.UpdateItemAsync(id, request, ct);
            return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
        }
        catch (PortfolioConflictException exception) { return Conflict(exception); }
    }

    private static async Task<IResult> DeleteItemAsync(Guid id, IPortfolioService service, CancellationToken ct)
    {
        var result = await service.DeleteOrArchiveItemAsync(id, ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateCategoryAsync(SavePortfolioCategoryRequest request, IPortfolioService service, CancellationToken ct)
    {
        var errors = PortfolioValidator.Validate(request);
        if (errors.Count > 0) return Validation(errors);
        try
        {
            var category = await service.CreateCategoryAsync(request, ct);
            return TypedResults.Created($"/api/admin/portfolio/categories/{category.Id}", category);
        }
        catch (PortfolioConflictException exception) { return Conflict(exception); }
    }

    private static async Task<IResult> UpdateCategoryAsync(Guid id, SavePortfolioCategoryRequest request, IPortfolioService service, CancellationToken ct)
    {
        var errors = PortfolioValidator.Validate(request);
        if (errors.Count > 0) return Validation(errors);
        try
        {
            var category = await service.UpdateCategoryAsync(id, request, ct);
            return category is null ? TypedResults.NotFound() : TypedResults.Ok(category);
        }
        catch (PortfolioConflictException exception) { return Conflict(exception); }
    }

    private static async Task<IResult> DeleteCategoryAsync(Guid id, IPortfolioService service, CancellationToken ct)
    {
        var result = await service.DeleteOrArchiveCategoryAsync(id, ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> UploadImageAsync(HttpRequest request, IUploadService service, CancellationToken ct)
    {
        if (!request.HasFormContentType) return FieldProblem("file", "A multipart/form-data request is required.");
        try
        {
            var form = await request.ReadFormAsync(ct);
            if (form.Files.Count != 1) return FieldProblem("file", "Select exactly one portfolio image.");
            var file = form.Files[0];
            await using var stream = file.OpenReadStream();
            var result = await service.UploadPortfolioImageAsync(new UploadFileRequest(file.FileName, file.ContentType, file.Length, stream), ct);
            return TypedResults.Ok(result);
        }
        catch (InvalidDataException) { return FieldProblem("file", "The upload request exceeds the configured size limit."); }
        catch (UploadValidationException exception) { return FieldProblem("file", exception.Message); }
    }

    private static async Task<IResult> OpenPublicImageAsync(Guid id, IUploadService service, CancellationToken ct)
    {
        var file = await service.OpenPublicPortfolioImageAsync(id, ct);
        return file is null ? TypedResults.NotFound() : Results.File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    private static async Task<IResult> OpenAdminImageAsync(Guid id, IUploadService service, CancellationToken ct)
    {
        var file = await service.OpenPortfolioImageForAdminAsync(id, ct);
        return file is null ? TypedResults.NotFound() : Results.File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    private static IResult Validation(IReadOnlyDictionary<string, string[]> errors) =>
        TypedResults.ValidationProblem(errors.ToDictionary(pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key), pair => pair.Value));
    private static IResult Conflict(PortfolioConflictException exception) => FieldProblem(exception.Field, exception.Message);
    private static IResult FieldProblem(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [JsonNamingPolicy.CamelCase.ConvertName(field)] = [message] });
}
