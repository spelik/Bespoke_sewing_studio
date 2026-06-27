namespace BespokeStudio.Application.Contracts.Uploads;

public sealed record UploadFileRequest(
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Stream Content);
