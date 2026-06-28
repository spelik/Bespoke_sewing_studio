using BespokeStudio.Application.Contracts.Content;
namespace BespokeStudio.Application.Abstractions;
public interface IPageContentService
{
    Task<PublicPageContentResponse> GetPublicPageAsync(string pageKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPageContentResponse>> GetAdminContentAsync(CancellationToken cancellationToken = default);
    Task<AdminPageContentResponse?> GetAdminByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminPageContentResponse> CreateAsync(SavePageContentRequest request, CancellationToken cancellationToken = default);
    Task<AdminPageContentResponse?> UpdateAsync(Guid id, SavePageContentRequest request, CancellationToken cancellationToken = default);
    Task<DeletePageContentResponse?> DeleteOrArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
