using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Storage;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Services;

public sealed class StorageMaintenanceService : IStorageMaintenanceService
{
    private const int MaxListedItems = 200;
    private const int MaxFailedItems = 100;

    private readonly BespokeStudioDbContext _dbContext;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly IUploadStorage _storage;
    private readonly ILogger<StorageMaintenanceService> _logger;

    public StorageMaintenanceService(
        BespokeStudioDbContext dbContext,
        IAdminAuditLogService auditLogService,
        IUploadStorage storage,
        ILogger<StorageMaintenanceService> logger)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _storage = storage;
        _logger = logger;
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

        var physicalFiles = _storage.EnumerateFiles();
        var databaseKeys = databaseFiles
            .Select(file => NormalizeStorageKey(file.StorageKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var physicalKeys = physicalFiles
            .Select(file => file.StorageKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphanFiles = physicalFiles
            .Where(file => !databaseKeys.Contains(file.StorageKey))
            .OrderBy(file => file.StorageKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var missingFiles = databaseFiles
            .Where(file => !physicalKeys.Contains(NormalizeStorageKey(file.StorageKey)))
            .ToArray();
        var relatedInfo = await LoadRelatedInfoAsync(
            missingFiles.Select(file => file.Id).ToArray(),
            cancellationToken);
        var jobCounts = await _dbContext.UploadFileDeletionJobs
            .AsNoTracking()
            .GroupBy(job => job.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var failedJobs = await _dbContext.UploadFileDeletionJobs
            .AsNoTracking()
            .Where(job => job.Status == UploadFileDeletionJobStatus.Failed)
            .OrderByDescending(job => job.UpdatedAt)
            .Take(MaxListedItems)
            .Select(job => new FailedStorageCleanupJobResponse(
                job.Id,
                job.StorageKey,
                job.Reason,
                job.Attempts,
                job.MaxAttempts,
                job.LastError,
                job.NextAttemptAt,
                job.CreatedAt,
                job.UpdatedAt))
            .ToArrayAsync(cancellationToken);

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
                    file.StorageKey,
                    file.SizeBytes,
                    file.LastModifiedAtUtc))
                .ToArray(),
            MissingPhysicalFiles: missingFiles
                .Take(MaxListedItems)
                .Select(file => new MissingPhysicalFileResponse(
                    file.Id,
                    file.OriginalFileName,
                    GetSafeDisplayStorageKey(file.Id, file.StorageKey),
                    file.Purpose,
                    relatedInfo.GetValueOrDefault(file.Id)))
                .ToArray(),
            CleanupJobs: new StorageCleanupJobSummaryResponse(
                PendingCount: GetJobCount(jobCounts, UploadFileDeletionJobStatus.Pending),
                ProcessingCount: GetJobCount(jobCounts, UploadFileDeletionJobStatus.Processing),
                FailedCount: GetJobCount(jobCounts, UploadFileDeletionJobStatus.Failed),
                SucceededCount: GetJobCount(jobCounts, UploadFileDeletionJobStatus.Succeeded),
                SkippedCount: GetJobCount(jobCounts, UploadFileDeletionJobStatus.Skipped),
                FailedJobs: failedJobs));
    }

    public async Task<StorageCleanupResponse> DeleteOrphansAsync(
        AdminAuditActor actor,
        CancellationToken cancellationToken = default)
    {
        var physicalFiles = _storage.EnumerateFiles();
        var referencedKeys = await LoadReferencedKeysAsync(cancellationToken);
        var candidates = physicalFiles
            .Where(file => !referencedKeys.Contains(file.StorageKey))
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
                if (referencedKeys.Contains(candidate.StorageKey))
                {
                    skippedCount++;
                    continue;
                }

                if (!_storage.Exists(candidate.StorageKey))
                {
                    skippedCount++;
                    continue;
                }

                var sizeBytes = _storage.GetFileSize(candidate.StorageKey);
                _storage.DeleteFile(candidate.StorageKey);
                deletedCount++;
                deletedBytes += sizeBytes;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to delete orphan physical upload {StorageKey}.",
                    candidate.StorageKey);

                if (failures.Count < MaxFailedItems)
                {
                    failures.Add(new StorageCleanupFailureResponse(
                        candidate.StorageKey,
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
            _storage.NormalizeAndValidateStorageKey(normalized);
            return normalized;
        }
        catch
        {
            return $"invalid-storage-key/{id:N}";
        }
    }

    private static string NormalizeStorageKey(string storageKey) =>
        storageKey.Replace('\\', '/');

    private static int GetJobCount(
        IReadOnlyDictionary<UploadFileDeletionJobStatus, int> counts,
        UploadFileDeletionJobStatus status) =>
        counts.GetValueOrDefault(status);

    private sealed record DatabaseFileSnapshot(
        Guid Id,
        string OriginalFileName,
        string StorageKey,
        string Purpose);
}
