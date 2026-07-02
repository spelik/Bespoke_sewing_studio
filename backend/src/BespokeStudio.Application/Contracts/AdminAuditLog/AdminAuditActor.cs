namespace BespokeStudio.Application.Contracts.AdminAuditLog;

public sealed record AdminAuditActor(
    Guid? UserId,
    string Email);
