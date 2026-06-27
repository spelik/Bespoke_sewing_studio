using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class UploadedFileMetadata
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public UploadPurpose Purpose { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StoredFileName { get; set; }
    public required string StorageKey { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
