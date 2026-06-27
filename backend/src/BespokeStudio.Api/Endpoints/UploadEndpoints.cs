using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(
        this IEndpointRouteBuilder endpoints,
        string publicBasePath)
    {
        var uploads = endpoints.MapGroup(publicBasePath.TrimEnd('/'))
            .WithTags("Uploads");

        uploads.MapPost("/order-attachments", UploadOrderAttachmentsAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithName("UploadOrderAttachments")
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<IReadOnlyList<UploadedFileResponse>>()
            .ProducesValidationProblem();

        uploads.MapGet("/{uploadedFileId:guid}", DownloadOrderAttachmentAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("DownloadOrderAttachment")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> UploadOrderAttachmentsAsync(
        HttpRequest request,
        IUploadService uploadService,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return ValidationProblem("files", "A multipart/form-data request is required.");
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return ValidationProblem("files", "The upload request exceeds the configured size limit.");
        }

        var streams = new List<Stream>(form.Files.Count);
        try
        {
            var files = form.Files.Select(file =>
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                return new UploadFileRequest(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream);
            }).ToArray();

            var uploadedFiles = await uploadService.UploadOrderAttachmentsAsync(files, cancellationToken);
            return TypedResults.Ok(uploadedFiles);
        }
        catch (UploadValidationException exception)
        {
            return ValidationProblem("files", exception.Message);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    private static async Task<IResult> DownloadOrderAttachmentAsync(
        Guid uploadedFileId,
        IUploadService uploadService,
        CancellationToken cancellationToken)
    {
        var file = await uploadService.OpenOrderAttachmentAsync(uploadedFileId, cancellationToken);
        return file is null
            ? TypedResults.NotFound()
            : Results.File(
                file.Content,
                file.ContentType,
                enableRangeProcessing: true);
    }

    private static IResult ValidationProblem(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [JsonNamingPolicy.CamelCase.ConvertName(field)] = [message]
        });
}
