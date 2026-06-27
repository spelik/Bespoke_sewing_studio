using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class UploadCleanupService : IUploadCleanupService
{
    private readonly BespokeStudioDbContext _dbContext;
    private readonly ILogger<UploadCleanupService> _logger;
    private readonly UploadStorageOptions _options;
    private readonly string _storageRoot;

    public UploadCleanupService(
        BespokeStudioDbContext dbContext,
        IOptions<UploadStorageOptions> options,
        IHostEnvironment environment,
        ILogger<UploadCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options.Value;
        _storageRoot = UploadStoragePath.ResolveRoot(_options, environment);
    }

    public async Task<UploadCleanupResponse> CleanupOrphansAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-_options.OrphanCleanupAgeMinutes);
        var candidateIds = await _dbContext.UploadedFiles
            .AsNoTracking()
            .Where(file =>
                file.Purpose == UploadPurpose.OrderAttachment &&
                file.CreatedAt < cutoff)
            .Select(file => file.Id)
            .ToArrayAsync(cancellationToken);

        var deletedMetadata = 0;
        var deletedPhysicalFiles = 0;
        var missingFiles = 0;
        var skipped = 0;

        foreach (var candidateId in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var transaction = await _dbContext.Database
                    .BeginTransactionAsync(cancellationToken);

                var metadata = await _dbContext.UploadedFiles
                    .SingleOrDefaultAsync(file =>
                        file.Id == candidateId &&
                        file.Purpose == UploadPurpose.OrderAttachment &&
                        file.CreatedAt < cutoff,
                        cancellationToken);

                if (metadata is null || await _dbContext.OrderAttachments
                        .AsNoTracking()
                        .AnyAsync(
                            attachment => attachment.UploadedFileId == candidateId,
                            cancellationToken))
                {
                    skipped++;
                    continue;
                }

                var physicalPath = UploadStoragePath.ResolveFile(
                    _storageRoot,
                    metadata.StorageKey);
                var fileExists = File.Exists(physicalPath);

                _dbContext.UploadedFiles.Remove(metadata);
                await _dbContext.SaveChangesAsync(cancellationToken);

                if (fileExists)
                {
                    File.Delete(physicalPath);
                    deletedPhysicalFiles++;
                }
                else
                {
                    missingFiles++;
                }

                await transaction.CommitAsync(cancellationToken);
                deletedMetadata++;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                skipped++;
                _logger.LogWarning(
                    exception,
                    "Failed to clean orphan upload {UploadedFileId}.",
                    candidateId);
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }
        }

        var response = new UploadCleanupResponse(
            ScannedCount: candidateIds.Length,
            DeletedMetadataCount: deletedMetadata,
            DeletedPhysicalFilesCount: deletedPhysicalFiles,
            MissingFilesCount: missingFiles,
            SkippedCount: skipped);

        _logger.LogInformation(
            "Orphan upload cleanup completed. Scanned {ScannedCount}, deleted metadata {DeletedMetadataCount}, deleted files {DeletedPhysicalFilesCount}, missing files {MissingFilesCount}, skipped {SkippedCount}.",
            response.ScannedCount,
            response.DeletedMetadataCount,
            response.DeletedPhysicalFilesCount,
            response.MissingFilesCount,
            response.SkippedCount);

        return response;
    }
}
