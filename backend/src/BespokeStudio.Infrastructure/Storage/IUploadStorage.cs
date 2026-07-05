namespace BespokeStudio.Infrastructure.Storage;

/// <summary>
/// Abstraction over the physical upload storage backend. The only implementation today is
/// <see cref="LocalUploadStorage"/> (local filesystem); a production object-storage adapter
/// (S3/Azure/R2) remains a future task. Callers work exclusively with relative, safe storage
/// keys and never with absolute server paths.
/// </summary>
public interface IUploadStorage
{
    /// <summary>
    /// Normalizes an incoming storage key to a safe relative key (forward slashes) and rejects
    /// absolute paths, drive paths and <c>../</c> traversal that would escape the storage root.
    /// </summary>
    string NormalizeAndValidateStorageKey(string storageKey);

    /// <summary>
    /// Builds the final and quarantine storage keys plus the generated stored file name for a
    /// new upload. Layout: <c>{folder}/yyyy/MM/{guid}{ext}</c> and
    /// <c>quarantine/{folder}/yyyy/MM/{guid}{ext}</c>.
    /// </summary>
    UploadStorageKeys BuildStorageKeys(string storageFolder, string extension, DateTimeOffset timestampUtc);

    /// <summary>
    /// Writes a brand new file (fails if the key already exists), enforcing the maximum byte
    /// limit while copying. Creates parent directories as needed.
    /// </summary>
    Task WriteNewFileAsync(string storageKey, Stream content, long maxBytes, CancellationToken cancellationToken);

    /// <summary>Opens an existing file for reading.</summary>
    Stream OpenRead(string storageKey);

    /// <summary>Returns whether a file exists for the given storage key.</summary>
    bool Exists(string storageKey);

    /// <summary>Returns the size, in bytes, of an existing file.</summary>
    long GetFileSize(string storageKey);

    /// <summary>Moves a quarantine file to its final location, creating parent directories.</summary>
    void MoveToFinal(string sourceStorageKey, string destinationStorageKey);

    /// <summary>
    /// Deletes a file, mirroring <see cref="System.IO.File.Delete(string)"/> semantics (no-op if
    /// the file is already missing, throws on I/O errors so callers can record failures).
    /// </summary>
    void DeleteFile(string storageKey);

    /// <summary>
    /// Best-effort delete used for cleanup of leftovers: swallows and logs I/O failures and
    /// returns whether the file is gone. Never throws for a missing file.
    /// </summary>
    bool DeleteIfExists(string storageKey);

    /// <summary>Enumerates the physical files under the storage root as safe relative keys.</summary>
    IReadOnlyList<UploadStorageFileInfo> EnumerateFiles();

    /// <summary>
    /// Resolves the absolute local filesystem path for a storage key. Only intended for the
    /// local malware scanner which currently requires a physical path; never returned in API
    /// responses.
    /// </summary>
    string GetRequiredLocalPhysicalPath(string storageKey);
}

/// <summary>Storage keys produced for a new upload.</summary>
public sealed record UploadStorageKeys(string FinalKey, string QuarantineKey, string StoredFileName);

/// <summary>A physical file discovered during a storage scan, described by a safe relative key.</summary>
public sealed record UploadStorageFileInfo(string StorageKey, long SizeBytes, DateTimeOffset? LastModifiedAtUtc);
