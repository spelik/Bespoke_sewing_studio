using System.Security.Claims;
using BespokeStudio.Application.Contracts.AdminAuditLog;

namespace BespokeStudio.Api.Endpoints;

internal static class AdminAuditEndpointHelpers
{
    public static AdminAuditActor GetActor(ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorUserId = Guid.TryParse(idValue, out var parsedId) ? parsedId : (Guid?)null;
        var actorEmail = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.Identity?.Name
            ?? "unknown-admin";

        return new AdminAuditActor(actorUserId, actorEmail);
    }

    public static AdminAuditLogWriteRequest CreateAuditRequest(
        ClaimsPrincipal principal,
        string action,
        string entityType,
        string? entityId,
        string? entityLabel,
        string summary,
        string? metadataJson = null)
    {
        var actor = GetActor(principal);

        return new AdminAuditLogWriteRequest(
            actor.UserId,
            actor.Email,
            action,
            entityType,
            entityId,
            entityLabel,
            summary,
            metadataJson);
    }
}
