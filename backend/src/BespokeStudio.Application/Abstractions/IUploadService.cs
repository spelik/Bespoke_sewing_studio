using BespokeStudio.Application.Contracts.Orders;
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
    /// <summary>
    /// Quarantine → signature → ClamAV → promote. Does not persist UploadedFile metadata.
    /// Caller must link metadata in one DB transaction, then compensate the promoted file on failure.
    /// </summary>
    Task<PreparedUploadFile> PrepareInStockImageAsync(
        UploadFileRequest file,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort cleanup after a promoted file could not be linked in the database.
    /// Tries immediate safe delete, then durable deletion-outbox scheduling.
    /// </summary>
    Task CompensateOrphanedPromotedFileAsync(
        string storageKey,
        string? originalFileName,
        long? fileSizeBytes,
        CancellationToken cancellationToken = default);

    Task<UploadDownloadResponse?> OpenPublicInStockImageAsync(
        Guid imageId,
        CancellationToken cancellationToken = default);
    Task<UploadDownloadResponse?> OpenInStockImageForAdminAsync(
        Guid imageId,
        CancellationToken cancellationToken = default);
    Task<UploadedFileResponse> UploadContentImageAsync(UploadFileRequest file, CancellationToken cancellationToken = default);
    Task<UploadDownloadResponse?> OpenPublicContentImageAsync(Guid uploadedFileId, CancellationToken cancellationToken = default);
    Task<UploadDownloadResponse?> OpenContentImageForAdminAsync(Guid uploadedFileId, CancellationToken cancellationToken = default);
    Task<UploadedFileResponse> UploadBrandImageAsync(UploadFileRequest file, CancellationToken cancellationToken = default);
    Task<UploadDownloadResponse?> OpenPublicBrandImageAsync(Guid uploadedFileId, CancellationToken cancellationToken = default);
    Task<UploadDownloadResponse?> OpenBrandImageForAdminAsync(Guid uploadedFileId, CancellationToken cancellationToken = default);

    Task<UploadDownloadResponse?> OpenPublicPortfolioImageAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<UploadDownloadResponse?> OpenPortfolioImageForAdminAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<UploadDownloadResponse?> OpenOrderAttachmentAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<DeleteOrderAttachmentResult?> DeleteOrderAttachmentAsync(
        Guid orderId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<UploadMetadataResponse?> GetMetadataAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadedFileResponse>> GetAllAsync(
        UploadPurpose? purpose = null,
        CancellationToken cancellationToken = default);
}
