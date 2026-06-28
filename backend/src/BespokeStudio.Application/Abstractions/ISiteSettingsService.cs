using BespokeStudio.Application.Contracts.SiteSettings;

namespace BespokeStudio.Application.Abstractions;

public interface ISiteSettingsService
{
    Task<PublicSiteSettingsResponse> GetPublicSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSiteSettingsResponse> GetAdminSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<NotificationSettingsResponse> GetNotificationSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSiteSettingsResponse> UpdateSettingsAsync(
        UpdateSiteSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<PublicBrandSettingsResponse> GetPublicBrandSettingsAsync(CancellationToken cancellationToken = default);
    Task<AdminBrandSettingsResponse> GetAdminBrandSettingsAsync(CancellationToken cancellationToken = default);
    Task<AdminBrandSettingsResponse> UpdateBrandSettingsAsync(UpdateBrandSettingsRequest request, CancellationToken cancellationToken = default);
}
