using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.EmailDeliveryLog;
using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Notifications;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Notifications;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class EmailOutboxService(
    BespokeStudioDbContext dbContext,
    IAdminRealtimeNotifier realtimeNotifier,
    IOptions<EmailOutboxOptions> options,
    ILogger<EmailOutboxService> logger) : IEmailOutboxService
{
    public async Task<Guid> EnqueueAsync(
        EmailOutboxEnqueueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.HtmlBody) &&
            string.IsNullOrWhiteSpace(request.TextBody))
        {
            throw new ArgumentException("An email body is required.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;
        var logEntry = new EmailDeliveryLogEntry
        {
            Id = Guid.NewGuid(),
            MessageType = TrimRequired(request.MessageType, 120, "unknown_email"),
            RecipientEmail = TrimRequired(request.RecipientEmail, 320, "unknown-recipient"),
            Subject = TrimRequired(request.Subject, 320, "No subject"),
            Provider = "Outbox",
            Status = "Queued",
            SentExternally = false,
            ResultMessage = "Queued for background delivery.",
            RelatedEntityType = TrimOptional(request.RelatedEntityType, 120),
            RelatedEntityId = TrimOptional(request.RelatedEntityId, 120),
            RelatedEntityLabel = TrimOptional(request.RelatedEntityLabel, 320),
            CreatedAt = now,
            CompletedAt = null
        };

        var outboxMessage = new EmailOutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = logEntry.MessageType,
            RecipientEmail = logEntry.RecipientEmail,
            RecipientName = TrimOptional(request.RecipientName, 200),
            Subject = logEntry.Subject,
            HtmlBody = NormalizeBody(request.HtmlBody),
            TextBody = NormalizeBody(request.TextBody),
            Status = EmailOutboxStatus.Pending,
            Attempts = 0,
            MaxAttempts = options.Value.MaxAttempts,
            NextAttemptAt = now,
            RelatedEntityType = logEntry.RelatedEntityType,
            RelatedEntityId = logEntry.RelatedEntityId,
            RelatedEntityLabel = logEntry.RelatedEntityLabel,
            CorrelationId = TrimOptional(request.CorrelationId, 120),
            EmailDeliveryLogEntryId = logEntry.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.EmailDeliveryLogEntries.Add(logEntry);
        dbContext.EmailOutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync(cancellationToken);

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
                "Realtime notification failed after email outbox message {OutboxMessageId} was queued.",
                outboxMessage.Id));
        }

        return outboxMessage.Id;
    }

    public async Task<EmailDeliveryManualRetryResponse> QueueManualRetryAsync(
        Guid emailDeliveryLogEntryId,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.EmailOutboxMessages
            .SingleOrDefaultAsync(
                candidate => candidate.EmailDeliveryLogEntryId == emailDeliveryLogEntryId,
                cancellationToken);

        if (message is null)
        {
            throw new EmailOutboxMessageNotFoundException(emailDeliveryLogEntryId);
        }

        if (!EmailOutboxManualRetryPolicy.IsManualRetryEligible(message))
        {
            throw new EmailManualRetryNotAllowedException(emailDeliveryLogEntryId);
        }

        var now = DateTimeOffset.UtcNow;
        message.Status = EmailOutboxStatus.Pending;
        message.Attempts = 0;
        message.MaxAttempts = options.Value.MaxAttempts;
        message.NextAttemptAt = now;
        message.ProcessingStartedAt = null;
        message.SentAt = null;
        message.LastError = null;
        message.UpdatedAt = now;

        const string resultMessage = "Manual retry queued for background delivery.";
        var logEntry = await dbContext.EmailDeliveryLogEntries
            .SingleOrDefaultAsync(entry => entry.Id == emailDeliveryLogEntryId, cancellationToken);
        if (logEntry is not null)
        {
            logEntry.Provider = "Outbox";
            logEntry.Status = "Queued";
            logEntry.SentExternally = false;
            logEntry.ResultMessage = resultMessage;
            logEntry.ErrorMessage = null;
            logEntry.CompletedAt = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await realtimeNotifier.NotifyEmailDeliveryLogChangedAsync(
                emailDeliveryLogEntryId,
                logEntry?.RelatedEntityLabel ?? message.RelatedEntityLabel,
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            TryLog(() => logger.LogWarning(
                exception,
                "Realtime notification failed after a manual retry was queued for email outbox message {OutboxMessageId}.",
                message.Id));
        }

        return new EmailDeliveryManualRetryResponse(
            emailDeliveryLogEntryId,
            message.Id,
            logEntry?.Status ?? "Queued",
            resultMessage,
            message.MessageType,
            logEntry?.RelatedEntityLabel ?? message.RelatedEntityLabel,
            now);
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

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeBody(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static void TryLog(Action write)
    {
        try
        {
            write();
        }
        catch
        {
            // The durable enqueue already succeeded; logging is best-effort.
        }
    }
}
