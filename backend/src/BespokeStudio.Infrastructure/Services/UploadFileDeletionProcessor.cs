using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

internal interface IUploadFileDeletionProcessor
{
    Task<int> ProcessDueAsync(CancellationToken cancellationToken);
}

internal sealed class UploadFileDeletionProcessor : IUploadFileDeletionProcessor
{
    private const string SafeFailureMessage =
        "Physical file deletion failed. Check server logs and file permissions.";

    private readonly BespokeStudioDbContext _dbContext;
    private readonly UploadDeletionOptions _options;
    private readonly ILogger<UploadFileDeletionProcessor> _logger;
    private readonly string _storageRoot;

    public UploadFileDeletionProcessor(
        BespokeStudioDbContext dbContext,
        IOptions<UploadStorageOptions> storageOptions,
        IOptions<UploadDeletionOptions> deletionOptions,
        IHostEnvironment environment,
        ILogger<UploadFileDeletionProcessor> logger)
    {
        _dbContext = dbContext;
        _options = deletionOptions.Value;
        _logger = logger;
        _storageRoot = UploadStoragePath.ResolveRoot(storageOptions.Value, environment);
    }

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await RecoverInterruptedJobsAsync(now, cancellationToken);

        var dueIds = await _dbContext.UploadFileDeletionJobs
            .AsNoTracking()
            .Where(job =>
                (job.Status == UploadFileDeletionJobStatus.Pending ||
                 job.Status == UploadFileDeletionJobStatus.Failed) &&
                job.Attempts < job.MaxAttempts &&
                (job.NextAttemptAt == null || job.NextAttemptAt <= now))
            .OrderBy(job => job.NextAttemptAt)
            .ThenBy(job => job.CreatedAt)
            .Select(job => job.Id)
            .Take(_options.BatchSize)
            .ToArrayAsync(cancellationToken);

        var processed = 0;
        foreach (var jobId in dueIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimed = await _dbContext.UploadFileDeletionJobs
                .Where(job =>
                    job.Id == jobId &&
                    (job.Status == UploadFileDeletionJobStatus.Pending ||
                     job.Status == UploadFileDeletionJobStatus.Failed) &&
                    job.Attempts < job.MaxAttempts &&
                    (job.NextAttemptAt == null || job.NextAttemptAt <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(job => job.Status, UploadFileDeletionJobStatus.Processing)
                    .SetProperty(job => job.Attempts, job => job.Attempts + 1)
                    .SetProperty(job => job.LastAttemptAt, now)
                    .SetProperty(job => job.UpdatedAt, now),
                    cancellationToken);

            if (claimed == 0)
            {
                continue;
            }

            await ProcessClaimedJobAsync(jobId, cancellationToken);
            processed++;
        }

        return processed;
    }

    private async Task ProcessClaimedJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext.UploadFileDeletionJobs
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == jobId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        try
        {
            var storageKey = UploadStoragePath.NormalizeAndValidateStorageKey(
                _storageRoot,
                job.StorageKey);
            var physicalPath = UploadStoragePath.ResolveFile(_storageRoot, storageKey);

            if (!File.Exists(physicalPath))
            {
                await CompleteAsync(
                    jobId,
                    UploadFileDeletionJobStatus.Skipped,
                    now,
                    cancellationToken);
                _logger.LogInformation(
                    "Upload deletion job {JobId} completed because the file was already missing.",
                    jobId);
                return;
            }

            File.Delete(physicalPath);
            await CompleteAsync(
                jobId,
                UploadFileDeletionJobStatus.Succeeded,
                now,
                cancellationToken);
            _logger.LogInformation("Upload deletion job {JobId} succeeded.", jobId);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            var nextAttemptAt = job.Attempts >= job.MaxAttempts
                ? (DateTimeOffset?)null
                : now.AddSeconds(CalculateBackoffSeconds(job.Attempts));

            await _dbContext.UploadFileDeletionJobs
                .Where(candidate => candidate.Id == jobId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, UploadFileDeletionJobStatus.Failed)
                    .SetProperty(candidate => candidate.LastError, SafeFailureMessage)
                    .SetProperty(candidate => candidate.NextAttemptAt, nextAttemptAt)
                    .SetProperty(candidate => candidate.UpdatedAt, now),
                    cancellationToken);

            _logger.LogWarning(
                exception,
                "Upload deletion job {JobId} failed on attempt {Attempt} of {MaxAttempts}.",
                jobId,
                job.Attempts,
                job.MaxAttempts);
        }
    }

    private Task<int> CompleteAsync(
        Guid jobId,
        UploadFileDeletionJobStatus status,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken) =>
        _dbContext.UploadFileDeletionJobs
            .Where(job => job.Id == jobId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Status, status)
                .SetProperty(job => job.SucceededAt, completedAt)
                .SetProperty(job => job.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(job => job.LastError, (string?)null)
                .SetProperty(job => job.UpdatedAt, completedAt),
                cancellationToken);

    private Task<int> RecoverInterruptedJobsAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var staleBefore = now.AddMinutes(-_options.ProcessingTimeoutMinutes);
        return _dbContext.UploadFileDeletionJobs
            .Where(job =>
                job.Status == UploadFileDeletionJobStatus.Processing &&
                job.UpdatedAt < staleBefore)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Status, UploadFileDeletionJobStatus.Failed)
                .SetProperty(job => job.NextAttemptAt, now)
                .SetProperty(job => job.LastError, "Previous cleanup attempt was interrupted and will be retried.")
                .SetProperty(job => job.UpdatedAt, now),
                cancellationToken);
    }

    private double CalculateBackoffSeconds(int attempts)
    {
        var exponent = Math.Clamp(attempts - 1, 0, 10);
        return Math.Min(
            _options.BaseRetrySeconds * Math.Pow(2, exponent),
            TimeSpan.FromHours(24).TotalSeconds);
    }
}
