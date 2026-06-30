using BespokeStudio.Application.Contracts.AdminAuditLog;

namespace BespokeStudio.Application.Abstractions;

public interface IAdminAuditLogService
{
    Task<IReadOnlyList<AdminAuditLogEntryResponse>> GetAsync(
        AdminAuditLogQueryRequest request,
        CancellationToken cancellationToken = default);

    Task RecordAsync(
        AdminAuditLogWriteRequest request,
        CancellationToken cancellationToken = default);
}
