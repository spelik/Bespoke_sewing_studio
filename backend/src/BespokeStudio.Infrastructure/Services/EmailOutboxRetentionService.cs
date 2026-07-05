using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.EmailDeliveryLog;
using BespokeStudio.Application.Notifications;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Notifications;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class EmailOutboxRetentionService(
    BespokeStudioDbContext dbContext,
    IOptions<EmailOutboxRetentionOptions> options) : IEmailOutboxRetentionService
{
    public async Task<EmailOutboxRetentionSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var now = DateTimeOffset.UtcNow;
        var placeholder = settings.PurgedBodyPlaceholder;

        var succeededBodyCutoff = now.AddDays(-settings.SucceededBodyRetentionDays);
        var succeededMessageCutoff = now.AddDays(-settings.SucceededMessageRetentionDays);
        var skippedBodyCutoff = now.AddDays(-settings.SkippedBodyRetentionDays);
        var skippedMessageCutoff = now.AddDays(-settings.SkippedMessageRetentionDays);

        var messages = dbContext.EmailOutboxMessages.AsNoTracking();

        var succeededBodyPurgeCandidateCount = await messages.CountAsync(
            message => message.Status == EmailOutboxStatus.Succeeded
                && (message.SentAt ?? message.UpdatedAt) <= succeededBodyCutoff
                && (message.HtmlBody != null || message.TextBody != placeholder),
            cancellationToken);

        var skippedBodyPurgeCandidateCount = await messages.CountAsync(
            message => message.Status == EmailOutboxStatus.Skipped
                && message.UpdatedAt <= skippedBodyCutoff
                && (message.HtmlBody != null || message.TextBody != placeholder),
            cancellationToken);

        var succeededDeleteCandidateCount = await messages.CountAsync(
            message => message.Status == EmailOutboxStatus.Succeeded
                && (message.SentAt ?? message.UpdatedAt) <= succeededMessageCutoff,
            cancellationToken);

        var skippedDeleteCandidateCount = await messages.CountAsync(
            message => message.Status == EmailOutboxStatus.Skipped
                && message.UpdatedAt <= skippedMessageCutoff,
            cancellationToken);

        var failedRetainedCount = await messages.CountAsync(
            message => message.Status == EmailOutboxStatus.Failed,
            cancellationToken);

        var oldestSucceededSentAt = await messages
            .Where(message => message.Status == EmailOutboxStatus.Succeeded && message.SentAt != null)
            .OrderBy(message => message.SentAt)
            .Select(message => message.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        var summaryMessage = EmailOutboxRetentionPolicy.ResolveSummaryMessage(
            succeededBodyPurgeCandidateCount,
            skippedBodyPurgeCandidateCount,
            succeededDeleteCandidateCount,
            skippedDeleteCandidateCount);

        return new EmailOutboxRetentionSummaryResponse(
            settings.WorkerEnabled,
            settings.WorkerIntervalHours,
            settings.BatchSize,
            settings.SucceededBodyRetentionDays,
            settings.SucceededMessageRetentionDays,
            settings.SkippedBodyRetentionDays,
            settings.SkippedMessageRetentionDays,
            succeededBodyPurgeCandidateCount,
            skippedBodyPurgeCandidateCount,
            succeededDeleteCandidateCount,
            skippedDeleteCandidateCount,
            failedRetainedCount,
            oldestSucceededSentAt,
            now,
            summaryMessage);
    }

    public async Task<EmailOutboxRetentionCleanupResponse> RunCleanupAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var now = DateTimeOffset.UtcNow;
        var placeholder = settings.PurgedBodyPlaceholder;
        var batchSize = settings.BatchSize;

        var succeededMessageCutoff = now.AddDays(-settings.SucceededMessageRetentionDays);
        var skippedMessageCutoff = now.AddDays(-settings.SkippedMessageRetentionDays);
        var succeededBodyCutoff = now.AddDays(-settings.SucceededBodyRetentionDays);
        var skippedBodyCutoff = now.AddDays(-settings.SkippedBodyRetentionDays);

        var succeededDeletedCount = await DeleteBatchAsync(
            message => message.Status == EmailOutboxStatus.Succeeded
                && (message.SentAt ?? message.UpdatedAt) <= succeededMessageCutoff,
            batchSize,
            cancellationToken);

        var skippedDeletedCount = await DeleteBatchAsync(
            message => message.Status == EmailOutboxStatus.Skipped
                && message.UpdatedAt <= skippedMessageCutoff,
            batchSize,
            cancellationToken);

        var succeededBodyPurgedCount = await PurgeBodyBatchAsync(
            message => message.Status == EmailOutboxStatus.Succeeded
                && (message.SentAt ?? message.UpdatedAt) <= succeededBodyCutoff
                && (message.SentAt ?? message.UpdatedAt) > succeededMessageCutoff
                && (message.HtmlBody != null || message.TextBody != placeholder),
            batchSize,
            placeholder,
            now,
            cancellationToken);

        var skippedBodyPurgedCount = await PurgeBodyBatchAsync(
            message => message.Status == EmailOutboxStatus.Skipped
                && message.UpdatedAt <= skippedBodyCutoff
                && message.UpdatedAt > skippedMessageCutoff
                && (message.HtmlBody != null || message.TextBody != placeholder),
            batchSize,
            placeholder,
            now,
            cancellationToken);

        var resultMessage =
            $"Retention cleanup completed: {succeededBodyPurgedCount} succeeded bodies purged, " +
            $"{skippedBodyPurgedCount} skipped bodies purged, " +
            $"{succeededDeletedCount} succeeded messages deleted, " +
            $"{skippedDeletedCount} skipped messages deleted.";

        return new EmailOutboxRetentionCleanupResponse(
            succeededBodyPurgedCount,
            skippedBodyPurgedCount,
            succeededDeletedCount,
            skippedDeletedCount,
            now,
            resultMessage);
    }

    private async Task<int> DeleteBatchAsync(
        System.Linq.Expressions.Expression<Func<Domain.Entities.EmailOutboxMessage, bool>> predicate,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.EmailOutboxMessages
            .AsNoTracking()
            .Where(predicate)
            .OrderBy(message => message.UpdatedAt)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        return await dbContext.EmailOutboxMessages
            .Where(message => ids.Contains(message.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> PurgeBodyBatchAsync(
        System.Linq.Expressions.Expression<Func<Domain.Entities.EmailOutboxMessage, bool>> predicate,
        int batchSize,
        string placeholder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.EmailOutboxMessages
            .AsNoTracking()
            .Where(predicate)
            .OrderBy(message => message.UpdatedAt)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        return await dbContext.EmailOutboxMessages
            .Where(message => ids.Contains(message.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.HtmlBody, (string?)null)
                    .SetProperty(message => message.TextBody, placeholder)
                    .SetProperty(message => message.UpdatedAt, now),
                cancellationToken);
    }
}
