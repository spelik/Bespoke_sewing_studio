using BespokeStudio.Application.Contracts.Notifications;

namespace BespokeStudio.Application.Abstractions;

public interface IEmailDeliverySettingsService
{
    Task<AdminEmailDeliverySettingsResponse> GetAdminSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminEmailDeliverySettingsResponse> UpdateAdminSettingsAsync(
        UpdateEmailDeliverySettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<ResolvedEmailDeliverySettings> GetResolvedSettingsAsync(
        CancellationToken cancellationToken = default);
}
