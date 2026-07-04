using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Notifications;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Notifications;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

internal interface IEmailOutboxProcessor
{
    Task<int> ProcessDueAsync(CancellationToken cancellationToken);
}

internal sealed class EmailOutboxProcessor(
    BespokeStudioDbContext dbContext,
    IEmailNotificationSender emailSender,
    IAdminRealtimeNotifier realtimeNotifier,
    IOptions<EmailOutboxOptions> options,
    ILogger<EmailOutboxProcessor> logger) : IEmailOutboxProcessor
{
    private const string SafeFailureMessage =
        "Email delivery failed. Check server logs and email provider configuration.";

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await RecoverInterruptedMessagesAsync(now, cancellationToken);

        var dueIds = await dbContext.EmailOutboxMessages
            .AsNoTracking()
            .Where(message =>
                (message.Status == EmailOutboxStatus.Pending ||
                 message.Status == EmailOutboxStatus.Failed) &&
                message.Attempts < message.MaxAttempts &&
                (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.NextAttemptAt)
            .ThenBy(message => message.CreatedAt)
            .Select(message => message.Id)
            .Take(options.Value.BatchSize)
            .ToArrayAsync(cancellationToken);

        var processed = 0;
        foreach (var messageId in dueIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimed = await dbContext.EmailOutboxMessages
                .Where(message =>
                    message.Id == messageId &&
                    (message.Status == EmailOutboxStatus.Pending ||
                     message.Status == EmailOutboxStatus.Failed) &&
                    message.Attempts < message.MaxAttempts &&
                    (message.NextAttemptAt == null || message.NextAttemptAt <= now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.Status, EmailOutboxStatus.Processing)
                    .SetProperty(message => message.Attempts, message => message.Attempts + 1)
                    .SetProperty(message => message.ProcessingStartedAt, now)
                    .SetProperty(message => message.UpdatedAt, now),
                    cancellationToken);

            if (claimed == 0)
            {
                continue;
            }

            await ProcessClaimedMessageAsync(messageId, cancellationToken);
            processed++;
        }

        return processed;
    }

    private async Task ProcessClaimedMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.EmailOutboxMessages
            .SingleAsync(candidate => candidate.Id == messageId, cancellationToken);

        try
        {
            var body = message.TextBody ?? message.HtmlBody ?? string.Empty;
            var result = await emailSender.SendAsync(
                message.RecipientEmail,
                message.Subject,
                body,
                cancellationToken);

            if (result.Success)
            {
                await CompleteSuccessfullyAsync(message, result, cancellationToken);
                return;
            }

            await RecordFailureAsync(
                message,
                result.Provider,
                result.Message,
                null,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RecordFailureAsync(
                message,
                "Unknown",
                SafeFailureMessage,
                exception,
                cancellationToken);
        }
    }

    private async Task CompleteSuccessfullyAsync(
        EmailOutboxMessage message,
        Application.Contracts.Notifications.EmailNotificationResult result,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        message.Status = EmailOutboxStatus.Succeeded;
        message.NextAttemptAt = null;
        message.ProcessingStartedAt = null;
        message.SentAt = now;
        message.LastError = null;
        message.UpdatedAt = now;

        var logEntry = await GetLogEntryAsync(message, cancellationToken);
        if (logEntry is not null)
        {
            logEntry.Provider = TrimRequired(result.Provider, 80, "Unknown");
            logEntry.Status = "Sent";
            logEntry.SentExternally = result.SentExternally;
            logEntry.ResultMessage = TrimRequired(result.Message, 1000, "Email sent.");
            logEntry.ErrorMessage = null;
            logEntry.CompletedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyLogChangedAsync(logEntry, cancellationToken);
        TryLog(() => logger.LogInformation(
            "Email outbox message {OutboxMessageId} succeeded on attempt {Attempt}.",
            message.Id,
            message.Attempts));
    }

    private async Task RecordFailureAsync(
        EmailOutboxMessage message,
        string provider,
        string resultMessage,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var exhausted = message.Attempts >= message.MaxAttempts;
        message.Status = EmailOutboxStatus.Failed;
        message.NextAttemptAt = exhausted
            ? null
            : now.Add(EmailOutboxRetryPolicy.CalculateDelay(
                message.Attempts,
                options.Value.RetryBaseSeconds,
                options.Value.RetryMaxMinutes));
        message.ProcessingStartedAt = null;
        message.LastError = SafeFailureMessage;
        message.UpdatedAt = now;

        var logEntry = await GetLogEntryAsync(message, cancellationToken);
        if (logEntry is not null)
        {
            logEntry.Provider = TrimRequired(provider, 80, "Unknown");
            logEntry.Status = exhausted ? "Failed" : "Retrying";
            logEntry.SentExternally = false;
            logEntry.ResultMessage = TrimRequired(
                resultMessage,
                1000,
                SafeFailureMessage);
            logEntry.ErrorMessage = SafeFailureMessage;
            logEntry.CompletedAt = exhausted ? now : null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyLogChangedAsync(logEntry, cancellationToken);

        TryLog(() => logger.LogWarning(
            exception,
            "Email outbox message {OutboxMessageId} failed on attempt {Attempt} of {MaxAttempts}; retry scheduled: {RetryScheduled}.",
            message.Id,
            message.Attempts,
            message.MaxAttempts,
            !exhausted));
    }

    private Task<EmailDeliveryLogEntry?> GetLogEntryAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken) =>
        message.EmailDeliveryLogEntryId is null
            ? Task.FromResult<EmailDeliveryLogEntry?>(null)
            : dbContext.EmailDeliveryLogEntries.SingleOrDefaultAsync(
                entry => entry.Id == message.EmailDeliveryLogEntryId.Value,
                cancellationToken);

    private async Task NotifyLogChangedAsync(
        EmailDeliveryLogEntry? logEntry,
        CancellationToken cancellationToken)
    {
        if (logEntry is null)
        {
            return;
        }

        try
        {
            await realtimeNotifier.NotifyEmailDeliveryLogChangedAsync(
                logEntry.Id,
                logEntry.RelatedEntityLabel,
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            TryLog(() => logger.LogWarning(
                exception,
                "Realtime notification failed for email delivery log entry {EmailDeliveryLogEntryId}.",
                logEntry.Id));
        }
    }

    private Task<int> RecoverInterruptedMessagesAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var staleBefore = now.AddMinutes(-options.Value.ProcessingTimeoutMinutes);
        return dbContext.EmailOutboxMessages
            .Where(message =>
                message.Status == EmailOutboxStatus.Processing &&
                (message.ProcessingStartedAt == null ||
                 message.ProcessingStartedAt < staleBefore))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, EmailOutboxStatus.Failed)
                .SetProperty(message => message.NextAttemptAt, now)
                .SetProperty(message => message.ProcessingStartedAt, (DateTimeOffset?)null)
                .SetProperty(message => message.LastError, "Previous delivery attempt was interrupted and will be retried.")
                .SetProperty(message => message.UpdatedAt, now),
                cancellationToken);
    }

    private static string TrimRequired(string? value, int maxLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static void TryLog(Action write)
    {
        try
        {
            write();
        }
        catch
        {
            // A failing logging provider must not stop durable outbox processing.
        }
    }
}
