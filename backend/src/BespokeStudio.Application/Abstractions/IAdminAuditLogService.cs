using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Common;

namespace BespokeStudio.Application.Abstractions;

public interface IAdminAuditLogService
{
    Task<PagedResponse<AdminAuditLogEntryResponse>> GetAsync(
        AdminAuditLogQueryRequest request,
        CancellationToken cancellationToken = default);

    Task RecordAsync(
        AdminAuditLogWriteRequest request,
        CancellationToken cancellationToken = default);

    void AddPending(AdminAuditLogWriteRequest request);
}
