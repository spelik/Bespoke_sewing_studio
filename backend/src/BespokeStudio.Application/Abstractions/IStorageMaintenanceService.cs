using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Storage;

namespace BespokeStudio.Application.Abstractions;

public interface IStorageMaintenanceService
{
    Task<StorageScanResponse> ScanAsync(CancellationToken cancellationToken = default);

    Task<StorageCleanupResponse> DeleteOrphansAsync(
        AdminAuditActor actor,
        CancellationToken cancellationToken = default);
}
