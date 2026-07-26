using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Contracts.Uploads;

namespace BespokeStudio.Application.Abstractions;

public interface IInStockService
{
    Task<IReadOnlyList<PublicInStockItemResponse>> GetPublicItemsAsync(
        CancellationToken cancellationToken = default);

    Task<PublicInStockItemResponse?> GetPublicItemBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminInStockItemResponse>> GetAdminItemsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminInStockItemResponse?> GetAdminItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminInStockItemResponse> CreateItemAsync(
        SaveInStockItemRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminInStockItemResponse?> UpdateItemAsync(
        Guid id,
        SaveInStockItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ArchiveInStockItemResponse?> ArchiveItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ArchiveInStockItemResponse?> RestoreItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminInStockImageResponse?> AddImageAsync(
        Guid itemId,
        UploadFileRequest file,
        string? altText,
        int? displayOrder,
        CancellationToken cancellationToken = default);

    Task<AdminInStockImageResponse?> UpdateImageAsync(
        Guid itemId,
        Guid imageId,
        UpdateInStockImageRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminInStockImageResponse>?> ReorderImagesAsync(
        Guid itemId,
        ReorderInStockImagesRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteImageAsync(
        Guid itemId,
        Guid imageId,
        CancellationToken cancellationToken = default);
}
