using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed class AdminAuditLogService(BespokeStudioDbContext dbContext) : IAdminAuditLogService
{
    private const int DefaultTake = 100;
    private const int MaxTake = 200;

    public async Task<IReadOnlyList<AdminAuditLogEntryResponse>> GetAsync(
        AdminAuditLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var take = request.Take <= 0 ? DefaultTake : Math.Min(request.Take, MaxTake);
        var search = Normalize(request.Search);
        var action = Normalize(request.Action);
        var entityType = Normalize(request.EntityType);
        var actorEmail = Normalize(request.ActorEmail);

        var query = dbContext.AdminAuditLogEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(entry => entry.Action.ToLower().Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(entry => entry.EntityType.ToLower().Contains(entityType));
        }

        if (!string.IsNullOrWhiteSpace(actorEmail))
        {
            query = query.Where(entry => entry.ActorEmail.ToLower().Contains(actorEmail));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(entry =>
                entry.ActorEmail.ToLower().Contains(search) ||
                entry.Action.ToLower().Contains(search) ||
                entry.EntityType.ToLower().Contains(search) ||
                (entry.EntityId != null && entry.EntityId.ToLower().Contains(search)) ||
                (entry.EntityLabel != null && entry.EntityLabel.ToLower().Contains(search)) ||
                entry.Summary.ToLower().Contains(search));
        }

        return await query
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(take)
            .Select(entry => new AdminAuditLogEntryResponse(
                entry.Id,
                entry.ActorUserId,
                entry.ActorEmail,
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.EntityLabel,
                entry.Summary,
                entry.MetadataJson,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task RecordAsync(
        AdminAuditLogWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new AdminAuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = request.ActorUserId,
            ActorEmail = TrimToLength(request.ActorEmail, 320, "unknown-admin"),
            Action = TrimToLength(request.Action, 120, "unknown.action"),
            EntityType = TrimToLength(request.EntityType, 120, "Unknown"),
            EntityId = TrimOptionalToLength(request.EntityId, 120),
            EntityLabel = TrimOptionalToLength(request.EntityLabel, 320),
            Summary = TrimToLength(request.Summary, 1000, "Admin action recorded."),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? null : request.MetadataJson,
            CreatedAt = now
        };

        dbContext.AdminAuditLogEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
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
