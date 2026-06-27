namespace BespokeStudio.Infrastructure.Storage;

public sealed class UploadStorageOptions
{
    public const string SectionName = "UploadStorage";

    public string RootPath { get; init; } = "../../storage/uploads";
    public string PublicBasePath { get; init; } = "/api/uploads";
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
    public int MaxFilesPerRequest { get; init; } = 5;
    public int OrphanCleanupAgeMinutes { get; init; } = 120;
    public IReadOnlyList<string> AllowedContentTypes { get; init; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    ];
}
