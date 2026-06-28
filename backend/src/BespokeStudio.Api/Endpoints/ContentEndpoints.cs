using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Content;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var pub=endpoints.MapGroup("/api/content").WithTags("Content");
        pub.MapGet("/pages/{pageKey}",async(string pageKey,IPageContentService s,CancellationToken ct)=>TypedResults.Ok(await s.GetPublicPageAsync(pageKey,ct))).AllowAnonymous().WithName("GetPublicPageContent");
        pub.MapGet("/images/{id:guid}",OpenPublicImageAsync).AllowAnonymous().WithName("GetPublicContentImage");
        var admin=endpoints.MapGroup("/api/admin/content").RequireAuthorization(AdminAccess.PolicyName).WithTags("Admin Content");
        admin.MapGet(string.Empty,async(IPageContentService s,CancellationToken ct)=>TypedResults.Ok(await s.GetAdminContentAsync(ct))).WithName("GetAdminContent");
        admin.MapGet("/{id:guid}",GetAsync).WithName("GetAdminContentById");
        admin.MapPost(string.Empty,CreateAsync).WithName("CreateAdminContent").ProducesValidationProblem();
        admin.MapPatch("/{id:guid}",UpdateAsync).WithName("UpdateAdminContent").ProducesValidationProblem();
        admin.MapDelete("/{id:guid}",DeleteAsync).WithName("ArchiveAdminContent");
        admin.MapPost("/uploads",UploadAsync).DisableAntiforgery().Accepts<IFormFile>("multipart/form-data").WithName("UploadAdminContentImage").Produces<UploadedFileResponse>().ProducesValidationProblem();
        admin.MapGet("/images/{id:guid}",OpenAdminImageAsync).WithName("GetAdminContentImage");
        return endpoints;
    }
    private static async Task<IResult> GetAsync(Guid id,IPageContentService s,CancellationToken ct){var x=await s.GetAdminByIdAsync(id,ct);return x is null?TypedResults.NotFound():TypedResults.Ok(x);}
    private static async Task<IResult> CreateAsync(SavePageContentRequest r,IPageContentService s,CancellationToken ct){var e=PageContentValidator.Validate(r);if(e.Count>0)return Validation(e);try{var x=await s.CreateAsync(r,ct);return TypedResults.Created($"/api/admin/content/{x.Id}",x);}catch(PageContentConflictException ex){return Conflict(ex);}}
    private static async Task<IResult> UpdateAsync(Guid id,SavePageContentRequest r,IPageContentService s,CancellationToken ct){var e=PageContentValidator.Validate(r);if(e.Count>0)return Validation(e);try{var x=await s.UpdateAsync(id,r,ct);return x is null?TypedResults.NotFound():TypedResults.Ok(x);}catch(PageContentConflictException ex){return Conflict(ex);}}
    private static async Task<IResult> DeleteAsync(Guid id,IPageContentService s,CancellationToken ct){var x=await s.DeleteOrArchiveAsync(id,ct);return x is null?TypedResults.NotFound():TypedResults.Ok(x);}
    private static async Task<IResult> UploadAsync(HttpRequest request,IUploadService s,CancellationToken ct)
    {if(!request.HasFormContentType)return Problem("file","A multipart/form-data request is required.");try{var form=await request.ReadFormAsync(ct);if(form.Files.Count!=1)return Problem("file","Select exactly one content image.");var f=form.Files[0];await using var stream=f.OpenReadStream();return TypedResults.Ok(await s.UploadContentImageAsync(new UploadFileRequest(f.FileName,f.ContentType,f.Length,stream),ct));}catch(InvalidDataException){return Problem("file","The upload request exceeds the configured size limit.");}catch(UploadValidationException ex){return Problem("file",ex.Message);}}
    private static async Task<IResult> OpenPublicImageAsync(Guid id,IUploadService s,CancellationToken ct){var f=await s.OpenPublicContentImageAsync(id,ct);return f is null?TypedResults.NotFound():Results.File(f.Content,f.ContentType,enableRangeProcessing:true);}
    private static async Task<IResult> OpenAdminImageAsync(Guid id,IUploadService s,CancellationToken ct){var f=await s.OpenContentImageForAdminAsync(id,ct);return f is null?TypedResults.NotFound():Results.File(f.Content,f.ContentType,enableRangeProcessing:true);}
    private static IResult Validation(IReadOnlyDictionary<string,string[]> e)=>TypedResults.ValidationProblem(e.ToDictionary(x=>JsonNamingPolicy.CamelCase.ConvertName(x.Key),x=>x.Value));
    private static IResult Conflict(PageContentConflictException ex)=>Problem(ex.Field,ex.Message);
    private static IResult Problem(string field,string message)=>TypedResults.ValidationProblem(new Dictionary<string,string[]>{{JsonNamingPolicy.CamelCase.ConvertName(field),[message]}});
}
