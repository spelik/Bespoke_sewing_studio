using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Storage;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class UploadFileDeletionScheduler : IUploadFileDeletionScheduler
{
    private readonly BespokeStudioDbContext _dbContext;
    private readonly UploadDeletionOptions _options;
    private readonly IUploadStorage _storage;

    public UploadFileDeletionScheduler(
        BespokeStudioDbContext dbContext,
        IOptions<UploadDeletionOptions> deletionOptions,
        IUploadStorage storage)
    {
        _dbContext = dbContext;
        _options = deletionOptions.Value;
        _storage = storage;
    }

    public async Task ScheduleAsync(
        ScheduleUploadFileDeletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var storageKey = _storage.NormalizeAndValidateStorageKey(request.StorageKey);
        var existing = _dbContext.UploadFileDeletionJobs.Local
            .FirstOrDefault(job => string.Equals(
                job.StorageKey,
                storageKey,
                StringComparison.OrdinalIgnoreCase))
            ?? await _dbContext.UploadFileDeletionJobs
                .FirstOrDefaultAsync(
                    job => job.StorageKey.ToLower() == storageKey.ToLower() &&
                        (job.Status == UploadFileDeletionJobStatus.Pending ||
                         job.Status == UploadFileDeletionJobStatus.Processing ||
                         job.Status == UploadFileDeletionJobStatus.Succeeded ||
                         job.Status == UploadFileDeletionJobStatus.Skipped ||
                         (job.Status == UploadFileDeletionJobStatus.Failed &&
                          job.Attempts < job.MaxAttempts)),
                    cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == UploadFileDeletionJobStatus.Failed &&
                existing.Attempts < existing.MaxAttempts)
            {
                existing.Status = UploadFileDeletionJobStatus.Pending;
                existing.NextAttemptAt = DateTimeOffset.UtcNow;
                existing.LastError = null;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }

            return;
        }

        var now = DateTimeOffset.UtcNow;
        _dbContext.UploadFileDeletionJobs.Add(new UploadFileDeletionJob
        {
            StorageKey = storageKey,
            OriginalFileName = TrimOptional(request.OriginalFileName, 255),
            FileSizeBytes = request.FileSizeBytes,
            RelatedEntityType = TrimRequired(request.RelatedEntityType, 120, "UploadedFile"),
            RelatedEntityId = request.RelatedEntityId,
            Reason = TrimRequired(request.Reason, 120, "upload.deleted"),
            Status = UploadFileDeletionJobStatus.Pending,
            Attempts = 0,
            MaxAttempts = _options.MaxAttempts,
            NextAttemptAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

    }

    private static string TrimRequired(string? value, int maxLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var fileName = Path.GetFileName(value.Trim());
        return fileName.Length <= maxLength ? fileName : fileName[..maxLength];
    }
}
