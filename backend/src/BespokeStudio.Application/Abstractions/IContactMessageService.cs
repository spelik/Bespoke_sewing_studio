using BespokeStudio.Application.Contracts.ContactMessages;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Common;

namespace BespokeStudio.Application.Abstractions;

public interface IContactMessageService
{
    Task<ContactMessageResponse> CreateAsync(CreateContactMessageRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ContactMessageListItemResponse>> GetAsync(
        ContactMessageListQueryRequest request,
        CancellationToken cancellationToken = default);
    Task<ContactMessageResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContactMessageResponse?> UpdateStatusAsync(Guid id, UpdateContactMessageStatusRequest request, CancellationToken cancellationToken = default);
    Task<DeleteContactMessageResult?> DeleteAsync(Guid id, AdminAuditActor actor, CancellationToken cancellationToken = default);
}
