using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class LocalUploadService : IUploadService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedExtensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"],
            ["application/pdf"] = [".pdf"]
        };

    private readonly BespokeStudioDbContext _dbContext;
    private readonly UploadStorageOptions _options;
    private readonly string _storageRoot;

    public LocalUploadService(
        BespokeStudioDbContext dbContext,
        IOptions<UploadStorageOptions> options,
        IHostEnvironment environment)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _storageRoot = Path.GetFullPath(
            Path.IsPathRooted(_options.RootPath)
                ? _options.RootPath
                : Path.Combine(environment.ContentRootPath, _options.RootPath));
    }

    public async Task<IReadOnlyList<UploadedFileResponse>> UploadOrderAttachmentsAsync(
        IReadOnlyCollection<UploadFileRequest> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count is < 1 || files.Count > _options.MaxFilesPerRequest)
        {
            throw new UploadValidationException(
                $"Select between 1 and {_options.MaxFilesPerRequest} files.");
        }

        var preparedFiles = files.Select(ValidateAndPrepare).ToArray();
        var createdPaths = new List<string>(preparedFiles.Length);
        var metadataItems = new List<UploadedFileMetadata>(preparedFiles.Length);

        try
        {
            foreach (var file in preparedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativeDirectory = Path.Combine(
                    "order-attachments",
                    DateTime.UtcNow.ToString("yyyy"),
                    DateTime.UtcNow.ToString("MM"));
                var storedFileName = $"{Guid.NewGuid():N}{file.Extension}";
                var storageKey = Path.Combine(relativeDirectory, storedFileName)
                    .Replace(Path.DirectorySeparatorChar, '/');
                var directoryPath = Path.Combine(_storageRoot, relativeDirectory);
                var physicalPath = Path.Combine(directoryPath, storedFileName);

                Directory.CreateDirectory(directoryPath);
                createdPaths.Add(physicalPath);

                await using (var destination = new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await CopyWithLimitAsync(
                        file.Request.Content,
                        destination,
                        _options.MaxFileSizeBytes,
                        cancellationToken);
                }

                metadataItems.Add(new UploadedFileMetadata
                {
                    Purpose = UploadPurpose.OrderAttachment,
                    OriginalFileName = file.OriginalFileName,
                    StoredFileName = storedFileName,
                    StorageKey = storageKey,
                    ContentType = file.ContentType,
                    SizeBytes = file.Request.SizeBytes
                });
            }

            _dbContext.UploadedFiles.AddRange(metadataItems);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var path in createdPaths)
            {
                TryDelete(path);
            }

            throw;
        }

        return metadataItems.Select(ToResponse).ToArray();
    }

    public async Task<UploadDownloadResponse?> OpenOrderAttachmentAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await (
            from attachment in _dbContext.OrderAttachments.AsNoTracking()
            join file in _dbContext.UploadedFiles.AsNoTracking()
                on attachment.UploadedFileId equals file.Id
            where file.Id == uploadedFileId && file.Purpose == UploadPurpose.OrderAttachment
            select file)
            .SingleOrDefaultAsync(cancellationToken);

        if (metadata is null)
        {
            return null;
        }

        var physicalPath = ResolveStoragePath(metadata.StorageKey);
        if (!File.Exists(physicalPath))
        {
            return null;
        }

        var stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return new UploadDownloadResponse(
            stream,
            metadata.OriginalFileName,
            metadata.ContentType,
            metadata.SizeBytes);
    }

    public async Task<UploadMetadataResponse?> GetMetadataAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UploadedFiles
            .AsNoTracking()
            .Where(file => file.Id == uploadedFileId)
            .Select(file => new UploadMetadataResponse(
                file.Id,
                file.Purpose,
                file.OriginalFileName,
                file.StoredFileName,
                file.StorageKey,
                file.ContentType,
                file.SizeBytes,
                file.CreatedAt,
                file.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UploadedFileResponse>> GetAllAsync(
        UploadPurpose? purpose = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UploadedFiles.AsNoTracking();
        if (purpose is not null)
        {
            query = query.Where(file => file.Purpose == purpose);
        }

        return await query
            .OrderByDescending(file => file.CreatedAt)
            .Select(file => new UploadedFileResponse(
                file.Id,
                file.OriginalFileName,
                file.ContentType,
                file.SizeBytes,
                file.Purpose,
                file.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private PreparedUpload ValidateAndPrepare(UploadFileRequest request)
    {
        if (request.SizeBytes <= 0)
        {
            throw new UploadValidationException("Empty files are not allowed.");
        }

        if (request.SizeBytes > _options.MaxFileSizeBytes)
        {
            throw new UploadValidationException(
                $"Each file must be no larger than {_options.MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var contentType = request.ContentType.Trim().ToLowerInvariant();
        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase) ||
            !AllowedExtensions.TryGetValue(contentType, out var validExtensions))
        {
            throw new UploadValidationException($"File type '{contentType}' is not supported.");
        }

        var originalFileName = Path.GetFileName(request.OriginalFileName.Trim());
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new UploadValidationException("A valid file name is required.");
        }

        if (originalFileName.Length > 255)
        {
            throw new UploadValidationException("File names must not exceed 255 characters.");
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!validExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new UploadValidationException(
                $"The extension '{extension}' does not match content type '{contentType}'.");
        }

        return new PreparedUpload(request, originalFileName, contentType, extension);
    }

    private string ResolveStoragePath(string storageKey)
    {
        var candidate = Path.GetFullPath(Path.Combine(
            _storageRoot,
            storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _storageRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Stored upload path escapes the configured storage root.");
        }

        return candidate;
    }

    private static async Task CopyWithLimitAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
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

    private static UploadedFileResponse ToResponse(UploadedFileMetadata file) =>
        new(
            file.Id,
            file.OriginalFileName,
            file.ContentType,
            file.SizeBytes,
            file.Purpose,
            file.CreatedAt);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Preserve the original failure; orphan cleanup is an operational concern.
        }
    }

    private sealed record PreparedUpload(
        UploadFileRequest Request,
        string OriginalFileName,
        string ContentType,
        string Extension);
}
