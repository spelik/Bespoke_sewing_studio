namespace BespokeStudio.Application.Contracts.AdminAuditLog;

public sealed record AdminAuditLogWriteRequest(
    Guid? ActorUserId,
    string ActorEmail,
    string Action,
    string EntityType,
    string? EntityId,
    string? EntityLabel,
    string Summary,
    string? MetadataJson = null);
