using BespokeStudio.Application.Contracts.ProductionReadiness;

namespace BespokeStudio.Application.Abstractions;

public interface IProductionReadinessService
{
    Task<ProductionReadinessResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}
