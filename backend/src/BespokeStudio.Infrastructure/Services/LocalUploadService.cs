using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly IMalwareScanner _malwareScanner;
    private readonly ILogger<LocalUploadService> _logger;
    private readonly string _storageRoot;

    public LocalUploadService(
        BespokeStudioDbContext dbContext,
        IOptions<UploadStorageOptions> options,
        IHostEnvironment environment,
        IMalwareScanner malwareScanner,
        ILogger<LocalUploadService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _malwareScanner = malwareScanner;
        _logger = logger;
        _storageRoot = UploadStoragePath.ResolveRoot(_options, environment);
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
        var storedUploads = new List<StoredUpload>(preparedFiles.Length);

        try
        {
            foreach (var file in preparedFiles)
            {
                storedUploads.Add(await StoreValidatedFileAsync(
                    file,
                    UploadPurpose.OrderAttachment,
                    "order-attachments",
                    cancellationToken));
            }

            _dbContext.UploadedFiles.AddRange(storedUploads.Select(upload => upload.Metadata));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var upload in storedUploads)
            {
                TryDelete(upload.PhysicalPath);
            }

            throw;
        }

        return storedUploads.Select(upload => ToResponse(upload.Metadata)).ToArray();
    }

    public async Task<UploadedFileResponse> UploadPortfolioImageAsync(
        UploadFileRequest file,
        CancellationToken cancellationToken = default)
    {
        var prepared = ValidateAndPrepare(file);
        if (!prepared.ContentType.StartsWith("image/", StringComparison.Ordinal))
        {
            throw new UploadValidationException("Portfolio uploads must be JPG, PNG or WebP images.");
        }

        var storedUpload = await StoreValidatedFileAsync(
            prepared,
            UploadPurpose.PortfolioImage,
            "portfolio-images",
            cancellationToken);

        try
        {
            _dbContext.UploadedFiles.Add(storedUpload.Metadata);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(storedUpload.Metadata);
        }
        catch
        {
            TryDelete(storedUpload.PhysicalPath);
            throw;
        }
    }

    public async Task<UploadedFileResponse> UploadContentImageAsync(
        UploadFileRequest file,
        CancellationToken cancellationToken = default)
    {
        var prepared = ValidateAndPrepare(file);
        if (!prepared.ContentType.StartsWith("image/", StringComparison.Ordinal))
        {
            throw new UploadValidationException("Content uploads must be JPG, PNG or WebP images.");
        }

        var storedUpload = await StoreValidatedFileAsync(
            prepared,
            UploadPurpose.SiteAsset,
            "content-images",
            cancellationToken);

        try
        {
            _dbContext.UploadedFiles.Add(storedUpload.Metadata);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(storedUpload.Metadata);
        }
        catch
        {
            TryDelete(storedUpload.PhysicalPath);
            throw;
        }
    }

    public async Task<UploadedFileResponse> UploadBrandImageAsync(
        UploadFileRequest file,
        CancellationToken cancellationToken = default)
    {
        var prepared = ValidateAndPrepare(file);
        if (!prepared.ContentType.StartsWith("image/", StringComparison.Ordinal))
        {
            throw new UploadValidationException("Brand uploads must be JPG, PNG or WebP images.");
        }

        var storedUpload = await StoreValidatedFileAsync(
            prepared,
            UploadPurpose.BrandAsset,
            "brand-images",
            cancellationToken);

        try
        {
            _dbContext.UploadedFiles.Add(storedUpload.Metadata);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(storedUpload.Metadata);
        }
        catch
        {
            TryDelete(storedUpload.PhysicalPath);
            throw;
        }
    }

    public async Task<UploadDownloadResponse?> OpenPublicContentImageAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await (
            from content in _dbContext.PageContents.AsNoTracking()
            join file in _dbContext.UploadedFiles.AsNoTracking() on content.ImageFileId equals file.Id
            where file.Id == uploadedFileId &&
                file.Purpose == UploadPurpose.SiteAsset &&
                content.IsActive &&
                content.ArchivedAt == null &&
                file.ContentType.StartsWith("image/")
            select file)
            .FirstOrDefaultAsync(cancellationToken);

        return OpenFile(metadata);
    }

    public async Task<UploadDownloadResponse?> OpenContentImageForAdminAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.UploadedFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                file => file.Id == uploadedFileId &&
                    file.Purpose == UploadPurpose.SiteAsset &&
                    file.ContentType.StartsWith("image/"),
                cancellationToken);

        return OpenFile(metadata);
    }

    public async Task<UploadDownloadResponse?> OpenPublicBrandImageAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await (
            from settings in _dbContext.SiteSettings.AsNoTracking()
            join file in _dbContext.UploadedFiles.AsNoTracking() on uploadedFileId equals file.Id
            where settings.Id == SiteSettings.SingletonId &&
                file.Purpose == UploadPurpose.BrandAsset &&
                file.ContentType.StartsWith("image/") &&
                (settings.LogoFileId == uploadedFileId ||
                 settings.FaviconFileId == uploadedFileId ||
                 settings.DefaultOgImageFileId == uploadedFileId)
            select file)
            .SingleOrDefaultAsync(cancellationToken);

        return OpenFile(metadata);
    }

    public async Task<UploadDownloadResponse?> OpenBrandImageForAdminAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.UploadedFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                file => file.Id == uploadedFileId &&
                    file.Purpose == UploadPurpose.BrandAsset &&
                    file.ContentType.StartsWith("image/"),
                cancellationToken);

        return OpenFile(metadata);
    }

    public async Task<UploadDownloadResponse?> OpenPublicPortfolioImageAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await (
            from item in _dbContext.PortfolioItems.AsNoTracking()
            join category in _dbContext.PortfolioCategories.AsNoTracking() on item.CategoryId equals category.Id
            join file in _dbContext.UploadedFiles.AsNoTracking() on item.CoverImageFileId equals file.Id
            where file.Id == uploadedFileId &&
                file.Purpose == UploadPurpose.PortfolioImage &&
                item.IsActive &&
                item.ArchivedAt == null &&
                category.IsActive &&
                category.ArchivedAt == null &&
                file.ContentType.StartsWith("image/")
            select file)
            .SingleOrDefaultAsync(cancellationToken);

        return OpenFile(metadata);
    }

    public async Task<UploadDownloadResponse?> OpenPortfolioImageForAdminAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.UploadedFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                file => file.Id == uploadedFileId &&
                    file.Purpose == UploadPurpose.PortfolioImage &&
                    file.ContentType.StartsWith("image/"),
                cancellationToken);

        return OpenFile(metadata);
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

        return OpenFile(metadata);
    }


    public async Task<DeleteOrderAttachmentResult?> DeleteOrderAttachmentAsync(
        Guid orderId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachmentData = await (
            from attachment in _dbContext.OrderAttachments
            join file in _dbContext.UploadedFiles on attachment.UploadedFileId equals file.Id
            join order in _dbContext.Orders on attachment.OrderId equals order.Id
            where attachment.OrderId == orderId &&
                attachment.Id == attachmentId &&
                file.Purpose == UploadPurpose.OrderAttachment
            select new
            {
                Attachment = attachment,
                File = file,
                Order = order
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (attachmentData is null)
        {
            return null;
        }

        var physicalPath = UploadStoragePath.ResolveFile(_storageRoot, attachmentData.File.StorageKey);
        var originalFileName = attachmentData.File.OriginalFileName;
        var uploadedFileId = attachmentData.File.Id;

        _dbContext.OrderAttachments.Remove(attachmentData.Attachment);
        _dbContext.UploadedFiles.Remove(attachmentData.File);
        attachmentData.Order.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var physicalFileDeleted = TryDelete(physicalPath);

        return new DeleteOrderAttachmentResult(
            orderId,
            attachmentData.Order.ReferenceNumber,
            attachmentId,
            uploadedFileId,
            originalFileName,
            physicalFileDeleted);
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
                file.CreatedAt,
                file.ScanStatus,
                file.ScanProvider,
                file.ScannedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<StoredUpload> StoreValidatedFileAsync(
        PreparedUpload file,
        UploadPurpose purpose,
        string storageFolder,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var relativeDirectory = Path.Combine(
            storageFolder,
            now.ToString("yyyy"),
            now.ToString("MM"));
        var quarantineDirectory = Path.Combine(
            "quarantine",
            storageFolder,
            now.ToString("yyyy"),
            now.ToString("MM"));
        var storedFileName = $"{Guid.NewGuid():N}{file.Extension}";
        var storageKey = Path.Combine(relativeDirectory, storedFileName)
            .Replace(Path.DirectorySeparatorChar, '/');
        var finalDirectoryPath = Path.Combine(_storageRoot, relativeDirectory);
        var finalPhysicalPath = Path.Combine(finalDirectoryPath, storedFileName);
        var quarantineDirectoryPath = Path.Combine(_storageRoot, quarantineDirectory);
        var quarantinePhysicalPath = Path.Combine(quarantineDirectoryPath, storedFileName);

        try
        {
            Directory.CreateDirectory(quarantineDirectoryPath);

            await using (var destination = new FileStream(
                quarantinePhysicalPath,
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

            await ValidateFileSignatureAsync(
                quarantinePhysicalPath,
                file.ContentType,
                cancellationToken);

            var scan = await _malwareScanner.ScanAsync(
                quarantinePhysicalPath,
                cancellationToken);

            if (!scan.IsAccepted)
            {
                throw new UploadValidationException(ToUploadRejectionMessage(scan.Status));
            }

            Directory.CreateDirectory(finalDirectoryPath);
            File.Move(quarantinePhysicalPath, finalPhysicalPath);

            var metadata = new UploadedFileMetadata
            {
                Purpose = purpose,
                OriginalFileName = file.OriginalFileName,
                StoredFileName = storedFileName,
                StorageKey = storageKey,
                ContentType = file.ContentType,
                SizeBytes = file.Request.SizeBytes,
                ScanStatus = scan.Status,
                ScanProvider = scan.Provider,
                ScannedAt = scan.ScannedAt,
                ScanMessage = scan.Message,
                CreatedAt = now,
                UpdatedAt = now
            };

            return new StoredUpload(metadata, finalPhysicalPath);
        }
        catch
        {
            TryDelete(quarantinePhysicalPath);
            TryDelete(finalPhysicalPath);
            throw;
        }
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

    private static async Task ValidateFileSignatureAsync(
        string physicalPath,
        string contentType,
        CancellationToken cancellationToken)
    {
        var header = new byte[16];
        await using var stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16,
            useAsync: true);

        var read = await stream.ReadAsync(header, cancellationToken);
        var isValid = contentType switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 &&
                header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "application/pdf" => read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            _ => false
        };

        if (!isValid)
        {
            throw new UploadValidationException("The file contents do not match the selected file type.");
        }
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
            file.CreatedAt,
            file.ScanStatus,
            file.ScanProvider,
            file.ScannedAt);

    private UploadDownloadResponse? OpenFile(UploadedFileMetadata? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        var physicalPath = UploadStoragePath.ResolveFile(_storageRoot, metadata.StorageKey);
        if (!File.Exists(physicalPath))
        {
            return null;
        }

        return new UploadDownloadResponse(
            new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true),
            metadata.OriginalFileName,
            metadata.ContentType,
            metadata.SizeBytes);
    }

    private static string ToUploadRejectionMessage(UploadScanStatus status) => status switch
    {
        UploadScanStatus.Infected => "This file could not be accepted because the security scan did not pass.",
        UploadScanStatus.ScanFailed => "This file could not be checked at the moment. Please try again later.",
        _ => "This file could not be accepted. Please upload a different file."
    };

    private bool TryDelete(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return true;
            }

            File.Delete(path);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to delete upload file {Path}.", path);
            return false;
        }
    }

    private sealed record PreparedUpload(
        UploadFileRequest Request,
        string OriginalFileName,
        string ContentType,
        string Extension);

    private sealed record StoredUpload(
        UploadedFileMetadata Metadata,
        string PhysicalPath);
}
