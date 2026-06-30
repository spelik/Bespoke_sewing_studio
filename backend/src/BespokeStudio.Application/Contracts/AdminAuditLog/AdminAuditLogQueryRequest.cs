namespace BespokeStudio.Application.Contracts.AdminAuditLog;

public sealed record AdminAuditLogQueryRequest(
    int Take,
    string? Search,
    string? Action,
    string? EntityType,
    string? ActorEmail);
