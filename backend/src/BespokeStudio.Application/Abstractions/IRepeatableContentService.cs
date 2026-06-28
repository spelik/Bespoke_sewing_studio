using BespokeStudio.Application.Contracts.RepeatableContent;

namespace BespokeStudio.Application.Abstractions;

public interface IRepeatableContentService
{
    Task<IReadOnlyList<PublicRepeatableContentGroupResponse>> GetPublicGroupsAsync(CancellationToken cancellationToken = default);
    Task<PublicRepeatableContentGroupResponse> GetPublicGroupAsync(string groupKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminRepeatableContentItemResponse>> GetAdminItemsAsync(CancellationToken cancellationToken = default);
    Task<AdminRepeatableContentItemResponse?> GetAdminByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminRepeatableContentItemResponse> CreateAsync(SaveRepeatableContentItemRequest request, CancellationToken cancellationToken = default);
    Task<AdminRepeatableContentItemResponse?> UpdateAsync(Guid id, SaveRepeatableContentItemRequest request, CancellationToken cancellationToken = default);
    Task<DeleteRepeatableContentItemResponse?> DeleteOrArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
