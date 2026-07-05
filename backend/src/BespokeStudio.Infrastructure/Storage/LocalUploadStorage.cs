using BespokeStudio.Application.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Storage;

/// <summary>
/// Local filesystem implementation of <see cref="IUploadStorage"/>. All path resolution and
/// traversal protection is delegated to <see cref="UploadStoragePath"/> so the existing safety
/// rules are preserved unchanged.
/// </summary>
public sealed class LocalUploadStorage : IUploadStorage
{
    private const int CopyBufferSize = 81920;

    private readonly ILogger<LocalUploadStorage> _logger;
    private readonly string _storageRoot;

    public LocalUploadStorage(
        IOptions<UploadStorageOptions> options,
        IHostEnvironment environment,
        ILogger<LocalUploadStorage> logger)
    {
        _logger = logger;
        _storageRoot = UploadStoragePath.ResolveRoot(options.Value, environment);
    }

    public string NormalizeAndValidateStorageKey(string storageKey) =>
        UploadStoragePath.NormalizeAndValidateStorageKey(_storageRoot, storageKey);

    public UploadStorageKeys BuildStorageKeys(
        string storageFolder,
        string extension,
        DateTimeOffset timestampUtc)
    {
        var year = timestampUtc.ToString("yyyy");
        var month = timestampUtc.ToString("MM");
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var finalKey = string.Join('/', storageFolder, year, month, storedFileName);
        var quarantineKey = string.Join('/', "quarantine", storageFolder, year, month, storedFileName);
        return new UploadStorageKeys(finalKey, quarantineKey, storedFileName);
    }

    public async Task WriteNewFileAsync(
        string storageKey,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var physicalPath = ResolveFile(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        await using var destination = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            CopyBufferSize,
            useAsync: true);

        await CopyWithLimitAsync(content, destination, maxBytes, cancellationToken);
    }

    public Stream OpenRead(string storageKey) =>
        new FileStream(
            ResolveFile(storageKey),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            CopyBufferSize,
            useAsync: true);

    public bool Exists(string storageKey) => File.Exists(ResolveFile(storageKey));

    public long GetFileSize(string storageKey) => new FileInfo(ResolveFile(storageKey)).Length;

    public void MoveToFinal(string sourceStorageKey, string destinationStorageKey)
    {
        var source = ResolveFile(sourceStorageKey);
        var destination = ResolveFile(destinationStorageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Move(source, destination);
    }

    public void DeleteFile(string storageKey) => File.Delete(ResolveFile(storageKey));

    public bool DeleteIfExists(string storageKey)
    {
        try
        {
            var physicalPath = ResolveFile(storageKey);
            if (!File.Exists(physicalPath))
            {
                return true;
            }

            File.Delete(physicalPath);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to delete upload file for storage key {StorageKey}.",
                storageKey);
            return false;
        }
    }

    public IReadOnlyList<UploadStorageFileInfo> EnumerateFiles()
    {
        if (!Directory.Exists(_storageRoot))
        {
            return [];
        }

        var files = new List<UploadStorageFileInfo>();
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (var physicalPath in Directory.EnumerateFiles(
                     _storageRoot,
                     "*",
                     enumerationOptions))
        {
            try
            {
                var relativeKey = Path.GetRelativePath(_storageRoot, physicalPath)
                    .Replace('\\', '/');
                var verifiedPath = UploadStoragePath.ResolveFile(_storageRoot, relativeKey);
                var fileInfo = new FileInfo(verifiedPath);

                files.Add(new UploadStorageFileInfo(
                    relativeKey,
                    fileInfo.Length,
                    new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero)));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "A physical upload entry could not be inspected during storage scan.");
            }
        }

        return files;
    }

    public string GetRequiredLocalPhysicalPath(string storageKey) => ResolveFile(storageKey);

    private string ResolveFile(string storageKey) =>
        UploadStoragePath.ResolveFile(_storageRoot, storageKey);

    private static async Task CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[CopyBufferSize];
        long totalBytes = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalBytes += bytesRead;
            if (totalBytes > maxBytes)
            {
                throw new UploadValidationException("The uploaded file exceeds the configured size limit.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }
}
