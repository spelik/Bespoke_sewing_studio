namespace BespokeStudio.Application.Contracts.AdminAuditLog;

public sealed record AdminAuditLogEntryResponse(
    Guid Id,
    Guid? ActorUserId,
    string ActorEmail,
    string Action,
    string EntityType,
    string? EntityId,
    string? EntityLabel,
    string Summary,
    string? MetadataJson,
    DateTimeOffset CreatedAt);
