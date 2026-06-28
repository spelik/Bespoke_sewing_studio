using BespokeStudio.Application.Contracts.Portfolio;

namespace BespokeStudio.Application.Abstractions;

public interface IPortfolioService
{
    Task<IReadOnlyList<PublicPortfolioItemResponse>> GetPublicPortfolioAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicPortfolioCategoryResponse>> GetPublicCategoriesAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPortfolioItemResponse>> GetAdminItemsAsync(
        CancellationToken cancellationToken = default);
    Task<AdminPortfolioItemResponse?> GetAdminItemByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminPortfolioItemResponse> CreateItemAsync(SavePortfolioItemRequest request, CancellationToken cancellationToken = default);
    Task<AdminPortfolioItemResponse?> UpdateItemAsync(Guid id, SavePortfolioItemRequest request, CancellationToken cancellationToken = default);
    Task<DeletePortfolioResponse?> DeleteOrArchiveItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPortfolioCategoryResponse>> GetAdminCategoriesAsync(CancellationToken cancellationToken = default);
    Task<AdminPortfolioCategoryResponse> CreateCategoryAsync(SavePortfolioCategoryRequest request, CancellationToken cancellationToken = default);
    Task<AdminPortfolioCategoryResponse?> UpdateCategoryAsync(Guid id, SavePortfolioCategoryRequest request, CancellationToken cancellationToken = default);
    Task<DeletePortfolioResponse?> DeleteOrArchiveCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
