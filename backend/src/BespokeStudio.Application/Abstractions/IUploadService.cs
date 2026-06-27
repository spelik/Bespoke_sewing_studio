using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Abstractions;

public interface IUploadService
{
    Task<UploadMetadataResponse?> GetMetadataAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadedFileResponse>> GetAllAsync(
        UploadPurpose? purpose = null,
        CancellationToken cancellationToken = default);
}
