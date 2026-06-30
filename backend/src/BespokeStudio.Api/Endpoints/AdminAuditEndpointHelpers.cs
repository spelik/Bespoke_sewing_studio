using System.Security.Claims;
using BespokeStudio.Application.Contracts.AdminAuditLog;

namespace BespokeStudio.Api.Endpoints;

internal static class AdminAuditEndpointHelpers
{
    public static AdminAuditLogWriteRequest CreateAuditRequest(
        ClaimsPrincipal principal,
        string action,
        string entityType,
        string? entityId,
        string? entityLabel,
        string summary,
        string? metadataJson = null)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorUserId = Guid.TryParse(idValue, out var parsedId) ? parsedId : (Guid?)null;
        var actorEmail = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.Identity?.Name
            ?? "unknown-admin";

        return new AdminAuditLogWriteRequest(
            actorUserId,
            actorEmail,
            action,
            entityType,
            entityId,
            entityLabel,
            summary,
            metadataJson);
    }
}
