using BespokeStudio.Application.Contracts.Clients;

namespace BespokeStudio.Application.Abstractions;

public interface IClientService
{
    Task<ClientResponse?> GetByIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientListItemResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
