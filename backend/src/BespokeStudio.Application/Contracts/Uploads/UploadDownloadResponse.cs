namespace BespokeStudio.Application.Contracts.Uploads;

public sealed record UploadDownloadResponse(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);
