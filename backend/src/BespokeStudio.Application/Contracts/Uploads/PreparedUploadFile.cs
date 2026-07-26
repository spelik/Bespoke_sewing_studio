using BespokeStudio.Domain.Entities;

namespace BespokeStudio.Application.Contracts.Uploads;

/// <summary>
/// Result of quarantine → scan → promote. Metadata is not persisted yet;
/// the caller must add it in the same DB transaction as the owning entity link.
/// </summary>
public sealed record PreparedUploadFile(
    UploadedFileMetadata Metadata,
    string FinalStorageKey);
