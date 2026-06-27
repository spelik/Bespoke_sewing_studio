using BespokeStudio.Application.Contracts.Services;

namespace BespokeStudio.Application.Abstractions;

public interface IServiceOfferingService
{
    Task<IReadOnlyList<ServiceOfferingResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceOfferingResponse> CreateAsync(
        CreateServiceOfferingRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceOfferingResponse?> UpdateAsync(
        Guid serviceOfferingId,
        UpdateServiceOfferingRequest request,
        CancellationToken cancellationToken = default);
}
