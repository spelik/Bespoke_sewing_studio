using BespokeStudio.Application.Contracts.Uploads;

namespace BespokeStudio.Application.Abstractions;

public interface IUploadCleanupService
{
    Task<UploadCleanupResponse> CleanupOrphansAsync(
        CancellationToken cancellationToken = default);
}
