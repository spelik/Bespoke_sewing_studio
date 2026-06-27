using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.SiteSettings;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SiteSettingsEntity = BespokeStudio.Domain.Entities.SiteSettings;

namespace BespokeStudio.Infrastructure.Services;

public sealed class SiteSettingsService(BespokeStudioDbContext dbContext) : ISiteSettingsService
{
    public async Task<PublicSiteSettingsResponse> GetPublicSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToPublicResponse(settings);
    }

    public async Task<AdminSiteSettingsResponse> GetAdminSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToAdminResponse(settings);
    }

    public async Task<NotificationSettingsResponse> GetNotificationSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return new NotificationSettingsResponse(
            settings.PublicEmail,
            settings.PublicPhone,
            settings.EmailNotificationsEnabled,
            settings.WhatsAppNotificationsEnabled);
    }

    public async Task<AdminSiteSettingsResponse> UpdateSettingsAsync(
        UpdateSiteSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);

        settings.StudioName = request.StudioName.Trim();
        settings.SiteTagline = Normalize(request.SiteTagline);
        settings.PublicEmail = NormalizeEmail(request.Email);
        settings.PublicPhone = Normalize(request.Phone);
        settings.ContactButtonLabel = Normalize(request.ContactButtonLabel);
        settings.ContactIntroText = Normalize(request.ContactIntroText);
        settings.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        settings.WhatsAppNotificationsEnabled = request.WhatsAppNotificationsEnabled;
        settings.FacebookUrl = Normalize(request.FacebookUrl);
        settings.InstagramUrl = Normalize(request.InstagramUrl);
        settings.TikTokUrl = Normalize(request.TikTokUrl);
        settings.PinterestUrl = Normalize(request.PinterestUrl);
        settings.FooterText = Normalize(request.FooterText);
        settings.ServiceAreaText = Normalize(request.ServiceAreaText);
        settings.BusinessLegalName = Normalize(request.BusinessLegalName);
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(settings);
    }

    private async Task<SiteSettingsEntity> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.SiteSettings.SingleOrDefaultAsync(
            candidate => candidate.Id == SiteSettingsEntity.SingletonId,
            cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = CreateDefault();
        dbContext.SiteSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static SiteSettingsEntity CreateDefault() => new()
    {
        Id = SiteSettingsEntity.SingletonId,
        StudioName = "Bespoke Sewing Studio",
        SiteTagline = "Bespoke sewing, tailoring, dressmaking, alterations and memory bears.",
        PublicPhone = "074 6734 7194",
        ContactButtonLabel = "Send Enquiry",
        ContactIntroText = "Consultations and orders are arranged individually.",
        ServiceAreaText = "Appointments arranged individually.",
        FooterText = "Bespoke Sewing Studio. All rights reserved.",
        EmailNotificationsEnabled = false,
        WhatsAppNotificationsEnabled = false,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static PublicSiteSettingsResponse ToPublicResponse(SiteSettingsEntity settings) =>
        new(
            settings.StudioName,
            settings.SiteTagline,
            settings.PublicEmail,
            settings.PublicPhone,
            settings.ContactButtonLabel,
            settings.ContactIntroText,
            settings.FacebookUrl,
            settings.InstagramUrl,
            settings.TikTokUrl,
            settings.PinterestUrl,
            settings.FooterText,
            settings.ServiceAreaText);

    private static AdminSiteSettingsResponse ToAdminResponse(SiteSettingsEntity settings) =>
        new(
            settings.Id,
            settings.StudioName,
            settings.SiteTagline,
            settings.PublicEmail,
            settings.PublicPhone,
            settings.ContactButtonLabel,
            settings.ContactIntroText,
            settings.EmailNotificationsEnabled,
            settings.WhatsAppNotificationsEnabled,
            settings.FacebookUrl,
            settings.InstagramUrl,
            settings.TikTokUrl,
            settings.PinterestUrl,
            settings.FooterText,
            settings.ServiceAreaText,
            settings.BusinessLegalName,
            settings.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmail(string? value) =>
        Normalize(value)?.ToLowerInvariant();
}
