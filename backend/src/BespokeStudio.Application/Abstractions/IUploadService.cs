using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Abstractions;

public interface IUploadService
{
    Task<IReadOnlyList<UploadedFileResponse>> UploadOrderAttachmentsAsync(
        IReadOnlyCollection<UploadFileRequest> files,
        CancellationToken cancellationToken = default);

    Task<UploadedFileResponse> UploadPortfolioImageAsync(
        UploadFileRequest file,
        CancellationToken cancellationToken = default);

    Task<UploadDownloadResponse?> OpenPublicPortfolioImageAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<UploadDownloadResponse?> OpenPortfolioImageForAdminAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<UploadDownloadResponse?> OpenOrderAttachmentAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<UploadMetadataResponse?> GetMetadataAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadedFileResponse>> GetAllAsync(
        UploadPurpose? purpose = null,
        CancellationToken cancellationToken = default);
}
