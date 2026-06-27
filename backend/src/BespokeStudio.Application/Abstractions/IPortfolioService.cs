using BespokeStudio.Application.Contracts.Portfolio;

namespace BespokeStudio.Application.Abstractions;

public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioItemResponse>> GetItemsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortfolioCategoryResponse>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<PortfolioItemResponse> CreateAsync(
        CreatePortfolioItemRequest request,
        CancellationToken cancellationToken = default);

    Task<PortfolioItemResponse?> UpdateAsync(
        Guid portfolioItemId,
        UpdatePortfolioItemRequest request,
        CancellationToken cancellationToken = default);
}
