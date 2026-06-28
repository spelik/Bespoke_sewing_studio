using BespokeStudio.Application.Contracts.ContactMessages;

namespace BespokeStudio.Application.Abstractions;

public interface IContactMessageService
{
    Task<ContactMessageResponse> CreateAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContactMessageListItemResponse>> GetAllAsync(int take, CancellationToken cancellationToken = default);
    Task<ContactMessageResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContactMessageResponse?> UpdateStatusAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default);
}
