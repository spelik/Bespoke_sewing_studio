using BespokeStudio.Application.Contracts.SiteSettings;

namespace BespokeStudio.Application.Abstractions;

public interface ISiteSettingsService
{
    Task<PublicSiteSettingsResponse> GetPublicSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSiteSettingsResponse> GetAdminSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSiteSettingsResponse> UpdateSettingsAsync(
        UpdateSiteSettingsRequest request,
        CancellationToken cancellationToken = default);
}
