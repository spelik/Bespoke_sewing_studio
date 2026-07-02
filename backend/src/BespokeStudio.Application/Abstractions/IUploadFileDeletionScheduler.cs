using BespokeStudio.Application.Contracts.Storage;

namespace BespokeStudio.Application.Abstractions;

public interface IUploadFileDeletionScheduler
{
    Task ScheduleAsync(
        ScheduleUploadFileDeletionRequest request,
        CancellationToken cancellationToken = default);
}
