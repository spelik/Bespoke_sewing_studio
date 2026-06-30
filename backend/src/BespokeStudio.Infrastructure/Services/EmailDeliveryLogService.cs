using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.EmailDeliveryLog;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed class EmailDeliveryLogService(
    BespokeStudioDbContext dbContext,
    IAdminRealtimeNotifier realtimeNotifier) : IEmailDeliveryLogService
{
    private const int DefaultTake = 100;
    private const int MaxTake = 200;

    public async Task<IReadOnlyList<EmailDeliveryLogEntryResponse>> GetAsync(
        EmailDeliveryLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var take = request.Take <= 0 ? DefaultTake : Math.Min(request.Take, MaxTake);
        var search = Normalize(request.Search);
        var messageType = Normalize(request.MessageType);
        var status = Normalize(request.Status);
        var recipientEmail = Normalize(request.RecipientEmail);
        var provider = Normalize(request.Provider);

        var query = dbContext.EmailDeliveryLogEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(messageType))
        {
            query = query.Where(entry => entry.MessageType.ToLower().Contains(messageType));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(entry => entry.Status.ToLower().Contains(status));
        }

        if (!string.IsNullOrWhiteSpace(recipientEmail))
        {
            query = query.Where(entry => entry.RecipientEmail.ToLower().Contains(recipientEmail));
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            query = query.Where(entry => entry.Provider.ToLower().Contains(provider));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(entry =>
                entry.MessageType.ToLower().Contains(search) ||
                entry.RecipientEmail.ToLower().Contains(search) ||
                entry.Subject.ToLower().Contains(search) ||
                entry.Provider.ToLower().Contains(search) ||
                entry.Status.ToLower().Contains(search) ||
                entry.ResultMessage.ToLower().Contains(search) ||
                (entry.ErrorMessage != null && entry.ErrorMessage.ToLower().Contains(search)) ||
                (entry.RelatedEntityType != null && entry.RelatedEntityType.ToLower().Contains(search)) ||
                (entry.RelatedEntityId != null && entry.RelatedEntityId.ToLower().Contains(search)) ||
                (entry.RelatedEntityLabel != null && entry.RelatedEntityLabel.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(take)
            .Select(entry => new EmailDeliveryLogEntryResponse(
                entry.Id,
                entry.MessageType,
                entry.RecipientEmail,
                entry.Subject,
                entry.Provider,
                entry.Status,
                entry.SentExternally,
                entry.ResultMessage,
                entry.ErrorMessage,
                entry.RelatedEntityType,
                entry.RelatedEntityId,
                entry.RelatedEntityLabel,
                entry.CreatedAt,
                entry.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task RecordAsync(
        EmailDeliveryLogWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new EmailDeliveryLogEntry
        {
            Id = Guid.NewGuid(),
            MessageType = TrimToLength(request.MessageType, 120, "unknown_email"),
            RecipientEmail = TrimToLength(request.RecipientEmail, 320, "unknown-recipient"),
            Subject = TrimToLength(request.Subject, 320, "No subject"),
            Provider = TrimToLength(request.Provider, 80, "Unknown"),
            Status = TrimToLength(request.Status, 32, "Failed"),
            SentExternally = request.SentExternally,
            ResultMessage = TrimToLength(request.ResultMessage, 1000, "Email attempt recorded."),
            ErrorMessage = TrimOptionalToLength(request.ErrorMessage, 1000),
            RelatedEntityType = TrimOptionalToLength(request.RelatedEntityType, 120),
            RelatedEntityId = TrimOptionalToLength(request.RelatedEntityId, 120),
            RelatedEntityLabel = TrimOptionalToLength(request.RelatedEntityLabel, 320),
            CreatedAt = now,
            CompletedAt = request.CompletedAt ?? now
        };

        dbContext.EmailDeliveryLogEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyEmailDeliveryLogChangedAsync(
            entry.Id,
            entry.RelatedEntityLabel,
            cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string TrimToLength(string? value, int maxLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? TrimOptionalToLength(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
