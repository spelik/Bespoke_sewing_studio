using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.SiteSettings;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class BrandSettingsEndpoints
{
    public static IEndpointRouteBuilder MapBrandSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/brand-settings/public",async(ISiteSettingsService s,CancellationToken ct)=>TypedResults.Ok(await s.GetPublicBrandSettingsAsync(ct))).AllowAnonymous().WithTags("Brand Settings").WithName("GetPublicBrandSettings").Produces<PublicBrandSettingsResponse>();
        endpoints.MapGet("/api/brand/images/{id:guid}",OpenPublicImageAsync).AllowAnonymous().WithTags("Brand Settings").WithName("GetPublicBrandImage");
        var admin=endpoints.MapGroup("/api/admin/brand-settings").RequireAuthorization(AdminAccess.PolicyName).WithTags("Admin Brand Settings");
        admin.MapGet(string.Empty,async(ISiteSettingsService s,CancellationToken ct)=>TypedResults.Ok(await s.GetAdminBrandSettingsAsync(ct))).WithName("GetAdminBrandSettings");
        admin.MapPatch(string.Empty,UpdateAsync).WithName("UpdateAdminBrandSettings").ProducesValidationProblem();
        endpoints.MapPost("/api/admin/brand/uploads",UploadAsync).RequireAuthorization(AdminAccess.PolicyName).WithTags("Admin Brand Settings").DisableAntiforgery().Accepts<IFormFile>("multipart/form-data").WithName("UploadAdminBrandImage").Produces<UploadedFileResponse>().ProducesValidationProblem();
        endpoints.MapGet("/api/admin/brand/images/{id:guid}",OpenAdminImageAsync).RequireAuthorization(AdminAccess.PolicyName).WithTags("Admin Brand Settings").WithName("GetAdminBrandImage");
        return endpoints;
    }
    private static async Task<IResult> UpdateAsync(UpdateBrandSettingsRequest r,ISiteSettingsService s,CancellationToken ct)
    { var errors=BrandSettingsValidator.Validate(r); if(errors.Count>0)return Validation(errors); try{return TypedResults.Ok(await s.UpdateBrandSettingsAsync(r,ct));}catch(BrandSettingsConflictException ex){return Problem(ex.Field,ex.Message);} }
    private static async Task<IResult> UploadAsync(HttpRequest request,IUploadService s,CancellationToken ct)
    { if(!request.HasFormContentType)return Problem("file","A multipart/form-data request is required."); try{var form=await request.ReadFormAsync(ct);if(form.Files.Count!=1)return Problem("file","Select exactly one brand image.");var f=form.Files[0];await using var stream=f.OpenReadStream();return TypedResults.Ok(await s.UploadBrandImageAsync(new UploadFileRequest(f.FileName,f.ContentType,f.Length,stream),ct));}catch(InvalidDataException){return Problem("file","The upload request exceeds the configured size limit.");}catch(UploadValidationException ex){return Problem("file",ex.Message);} }
    private static async Task<IResult> OpenPublicImageAsync(Guid id,IUploadService s,CancellationToken ct){var f=await s.OpenPublicBrandImageAsync(id,ct);return f is null?TypedResults.NotFound():Results.File(f.Content,f.ContentType,enableRangeProcessing:true);}
    private static async Task<IResult> OpenAdminImageAsync(Guid id,IUploadService s,CancellationToken ct){var f=await s.OpenBrandImageForAdminAsync(id,ct);return f is null?TypedResults.NotFound():Results.File(f.Content,f.ContentType,enableRangeProcessing:true);}
    private static IResult Validation(IReadOnlyDictionary<string,string[]> e)=>TypedResults.ValidationProblem(e.ToDictionary(x=>JsonNamingPolicy.CamelCase.ConvertName(x.Key),x=>x.Value));
    private static IResult Problem(string field,string message)=>TypedResults.ValidationProblem(new Dictionary<string,string[]>{{JsonNamingPolicy.CamelCase.ConvertName(field),[message]}});
}
