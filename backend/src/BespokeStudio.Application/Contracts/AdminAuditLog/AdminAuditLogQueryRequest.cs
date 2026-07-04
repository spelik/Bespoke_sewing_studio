namespace BespokeStudio.Application.Contracts.AdminAuditLog;

public sealed record AdminAuditLogQueryRequest(
    int Page,
    int PageSize,
    string? Search,
    string? Action,
    string? EntityType,
    string? ActorEmail);
