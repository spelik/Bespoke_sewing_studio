using BespokeStudio.Application.Contracts.Services;

namespace BespokeStudio.Application.Abstractions;

public interface IServiceOfferingService
{
    Task<IReadOnlyList<PublicServiceOfferingResponse>> GetPublicServicesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminServiceOfferingResponse>> GetAdminServicesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminServiceOfferingResponse?> GetByIdAsync(
        Guid serviceOfferingId,
        CancellationToken cancellationToken = default);

    Task<AdminServiceOfferingResponse> CreateAsync(
        CreateServiceOfferingRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminServiceOfferingResponse?> UpdateAsync(
        Guid serviceOfferingId,
        UpdateServiceOfferingRequest request,
        CancellationToken cancellationToken = default);

    Task<DeleteServiceOfferingResponse?> DeleteOrArchiveAsync(
        Guid serviceOfferingId,
        CancellationToken cancellationToken = default);
}
