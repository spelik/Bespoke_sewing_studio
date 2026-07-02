using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Storage;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class StorageMaintenanceService : IStorageMaintenanceService
{
    private const int MaxListedItems = 200;
    private const int MaxFailedItems = 100;

    private readonly BespokeStudioDbContext _dbContext;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<StorageMaintenanceService> _logger;
    private readonly string _storageRoot;

    public StorageMaintenanceService(
        BespokeStudioDbContext dbContext,
        IAdminAuditLogService auditLogService,
        IOptions<UploadStorageOptions> options,
        IHostEnvironment environment,
        ILogger<StorageMaintenanceService> logger)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _logger = logger;
        _storageRoot = UploadStoragePath.ResolveRoot(options.Value, environment);
    }

    public async Task<StorageScanResponse> ScanAsync(
        CancellationToken cancellationToken = default)
    {
        var databaseFiles = await _dbContext.UploadedFiles
            .AsNoTracking()
            .OrderBy(file => file.CreatedAt)
            .Select(file => new DatabaseFileSnapshot(
                file.Id,
                file.OriginalFileName,
                file.StorageKey,
                file.Purpose.ToString()))
            .ToArrayAsync(cancellationToken);

        var physicalFiles = EnumeratePhysicalFiles();
        var databaseKeys = databaseFiles
            .Select(file => NormalizeStorageKey(file.StorageKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var physicalKeys = physicalFiles
            .Select(file => file.RelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphanFiles = physicalFiles
            .Where(file => !databaseKeys.Contains(file.RelativePath))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var missingFiles = databaseFiles
            .Where(file => !physicalKeys.Contains(NormalizeStorageKey(file.StorageKey)))
            .ToArray();
        var relatedInfo = await LoadRelatedInfoAsync(
            missingFiles.Select(file => file.Id).ToArray(),
            cancellationToken);

        return new StorageScanResponse(
            DatabaseFileCount: databaseFiles.Length,
            PhysicalFileCount: physicalFiles.Count,
            TotalPhysicalBytes: physicalFiles.Sum(file => file.SizeBytes),
            OrphanPhysicalFileCount: orphanFiles.Length,
            OrphanPhysicalBytes: orphanFiles.Sum(file => file.SizeBytes),
            MissingPhysicalFileCount: missingFiles.Length,
            ScannedAt: DateTimeOffset.UtcNow,
            OrphanPhysicalFiles: orphanFiles
                .Take(MaxListedItems)
                .Select(file => new OrphanPhysicalFileResponse(
                    file.RelativePath,
                    file.SizeBytes,
                    file.LastModifiedAt))
                .ToArray(),
            MissingPhysicalFiles: missingFiles
                .Take(MaxListedItems)
                .Select(file => new MissingPhysicalFileResponse(
                    file.Id,
                    file.OriginalFileName,
                    GetSafeDisplayStorageKey(file.Id, file.StorageKey),
                    file.Purpose,
                    relatedInfo.GetValueOrDefault(file.Id)))
                .ToArray());
    }

    public async Task<StorageCleanupResponse> DeleteOrphansAsync(
        AdminAuditActor actor,
        CancellationToken cancellationToken = default)
    {
        var physicalFiles = EnumeratePhysicalFiles();
        var referencedKeys = await LoadReferencedKeysAsync(cancellationToken);
        var candidates = physicalFiles
            .Where(file => !referencedKeys.Contains(file.RelativePath))
            .ToArray();

        var deletedCount = 0;
        long deletedBytes = 0;
        var skippedCount = 0;
        var failures = new List<StorageCleanupFailureResponse>();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                referencedKeys = await LoadReferencedKeysAsync(cancellationToken);
                if (referencedKeys.Contains(candidate.RelativePath))
                {
                    skippedCount++;
                    continue;
                }

                var physicalPath = UploadStoragePath.ResolveFile(
                    _storageRoot,
                    candidate.RelativePath);
                if (!File.Exists(physicalPath))
                {
                    skippedCount++;
                    continue;
                }

                var sizeBytes = new FileInfo(physicalPath).Length;
                File.Delete(physicalPath);
                deletedCount++;
                deletedBytes += sizeBytes;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete orphan physical upload {StorageKey}.",
                    candidate.RelativePath);

                if (failures.Count < MaxFailedItems)
                {
                    failures.Add(new StorageCleanupFailureResponse(
                        candidate.RelativePath,
                        "The file could not be deleted. Check server logs and file permissions."));
                }
            }
        }

        var result = new StorageCleanupResponse(
            DeletedCount: deletedCount,
            DeletedBytes: deletedBytes,
            SkippedCount: skippedCount,
            FailedCount: candidates.Length - deletedCount - skippedCount,
            FailedItems: failures);

        await _auditLogService.RecordAsync(
            new AdminAuditLogWriteRequest(
                actor.UserId,
                actor.Email,
                "storage.orphan_cleanup",
                "Storage",
                null,
                "Local upload storage",
                $"Orphan storage cleanup deleted {result.DeletedCount} file(s) ({result.DeletedBytes} bytes), skipped {result.SkippedCount}, failed {result.FailedCount}."),
            cancellationToken);

        return result;
    }

    private IReadOnlyList<PhysicalFileSnapshot> EnumeratePhysicalFiles()
    {
        if (!Directory.Exists(_storageRoot))
        {
            return [];
        }

        var files = new List<PhysicalFileSnapshot>();
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
                var relativePath = NormalizeStorageKey(
                    Path.GetRelativePath(_storageRoot, physicalPath));
                var verifiedPath = UploadStoragePath.ResolveFile(_storageRoot, relativePath);
                var fileInfo = new FileInfo(verifiedPath);

                files.Add(new PhysicalFileSnapshot(
                    relativePath,
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

    private async Task<HashSet<string>> LoadReferencedKeysAsync(
        CancellationToken cancellationToken)
    {
        var keys = await _dbContext.UploadedFiles
            .AsNoTracking()
            .Select(file => file.StorageKey)
            .ToArrayAsync(cancellationToken);

        return keys
            .Select(NormalizeStorageKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<Guid, string>> LoadRelatedInfoAsync(
        IReadOnlyCollection<Guid> missingIds,
        CancellationToken cancellationToken)
    {
        if (missingIds.Count == 0)
        {
            return [];
        }

        var relatedInfo = new Dictionary<Guid, string>();

        var orders = await (
            from attachment in _dbContext.OrderAttachments.AsNoTracking()
            join order in _dbContext.Orders.AsNoTracking()
                on attachment.OrderId equals order.Id
            where missingIds.Contains(attachment.UploadedFileId)
            select new { attachment.UploadedFileId, order.ReferenceNumber })
            .ToArrayAsync(cancellationToken);
        foreach (var order in orders)
        {
            relatedInfo[order.UploadedFileId] = $"Order {order.ReferenceNumber}";
        }

        var portfolioItems = await _dbContext.PortfolioItems
            .AsNoTracking()
            .Where(item => item.CoverImageFileId != null && missingIds.Contains(item.CoverImageFileId.Value))
            .Select(item => new { FileId = item.CoverImageFileId!.Value, item.Title })
            .ToArrayAsync(cancellationToken);
        foreach (var item in portfolioItems)
        {
            relatedInfo[item.FileId] = $"Portfolio: {item.Title}";
        }

        var pageContent = await _dbContext.PageContents
            .AsNoTracking()
            .Where(content => content.ImageFileId != null && missingIds.Contains(content.ImageFileId.Value))
            .Select(content => new
            {
                FileId = content.ImageFileId!.Value,
                content.PageKey,
                content.SectionKey
            })
            .ToArrayAsync(cancellationToken);
        foreach (var content in pageContent)
        {
            relatedInfo[content.FileId] = $"Content: {content.PageKey}/{content.SectionKey}";
        }

        var settings = await _dbContext.SiteSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            AddRelatedSetting(relatedInfo, missingIds, settings.LogoFileId, "Site settings: logo");
            AddRelatedSetting(relatedInfo, missingIds, settings.FaviconFileId, "Site settings: favicon");
            AddRelatedSetting(relatedInfo, missingIds, settings.DefaultOgImageFileId, "Site settings: default social image");
        }

        return relatedInfo;
    }

    private static void AddRelatedSetting(
        IDictionary<Guid, string> relatedInfo,
        IReadOnlyCollection<Guid> missingIds,
        Guid? fileId,
        string label)
    {
        if (fileId.HasValue && missingIds.Contains(fileId.Value))
        {
            relatedInfo[fileId.Value] = label;
        }
    }

    private string GetSafeDisplayStorageKey(Guid id, string storageKey)
    {
        var normalized = NormalizeStorageKey(storageKey);
        try
        {
            UploadStoragePath.ResolveFile(_storageRoot, normalized);
            return normalized;
        }
        catch
        {
            return $"invalid-storage-key/{id:N}";
        }
    }

    private static string NormalizeStorageKey(string storageKey) =>
        storageKey.Replace('\\', '/');

    private sealed record DatabaseFileSnapshot(
        Guid Id,
        string OriginalFileName,
        string StorageKey,
        string Purpose);

    private sealed record PhysicalFileSnapshot(
        string RelativePath,
        long SizeBytes,
        DateTimeOffset? LastModifiedAt);
}
