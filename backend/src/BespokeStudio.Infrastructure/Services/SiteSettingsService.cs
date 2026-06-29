using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.SiteSettings;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Enums;
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
            settings.StudioName,
            settings.PublicEmail,
            settings.EmailNotificationsEnabled,
            settings.CustomerConfirmationEmailsEnabled,
            settings.CustomerOrderConfirmationSubject,
            settings.CustomerOrderConfirmationBody,
            settings.CustomerContactConfirmationSubject,
            settings.CustomerContactConfirmationBody);
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
        settings.CustomerConfirmationEmailsEnabled = request.CustomerConfirmationEmailsEnabled;
        settings.CustomerOrderConfirmationSubject = request.CustomerOrderConfirmationSubject.Trim();
        settings.CustomerOrderConfirmationBody = NormalizeLineEndings(request.CustomerOrderConfirmationBody.Trim());
        settings.CustomerContactConfirmationSubject = request.CustomerContactConfirmationSubject.Trim();
        settings.CustomerContactConfirmationBody = NormalizeLineEndings(request.CustomerContactConfirmationBody.Trim());
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

    public async Task<PublicBrandSettingsResponse> GetPublicBrandSettingsAsync(CancellationToken cancellationToken = default)
        => ToPublicBrand(await GetOrCreateAsync(cancellationToken));

    public async Task<AdminBrandSettingsResponse> GetAdminBrandSettingsAsync(CancellationToken cancellationToken = default)
        => ToAdminBrand(await GetOrCreateAsync(cancellationToken));

    public async Task<AdminBrandSettingsResponse> UpdateBrandSettingsAsync(UpdateBrandSettingsRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureBrandImageAsync(request.LogoFileId, nameof(request.LogoFileId), cancellationToken);
        await EnsureBrandImageAsync(request.FaviconFileId, nameof(request.FaviconFileId), cancellationToken);
        await EnsureBrandImageAsync(request.DefaultOgImageFileId, nameof(request.DefaultOgImageFileId), cancellationToken);
        var s=await GetOrCreateAsync(cancellationToken);
        s.BrandDisplayName=request.BrandDisplayName.Trim(); s.LogoFileId=request.LogoFileId; s.LogoAltText=request.LogoAltText.Trim();
        s.FaviconFileId=request.FaviconFileId; s.HeaderCtaLabel=request.HeaderCtaLabel.Trim(); s.HeaderCtaUrl=request.HeaderCtaUrl.Trim();
        s.DefaultMetaTitle=request.DefaultMetaTitle.Trim(); s.DefaultMetaDescription=request.DefaultMetaDescription.Trim();
        s.DefaultOgTitle=Normalize(request.DefaultOgTitle); s.DefaultOgDescription=Normalize(request.DefaultOgDescription); s.DefaultOgImageFileId=request.DefaultOgImageFileId;
        s.ShowServicesLink=request.ShowServicesLink; s.ServicesLabel=request.ServicesLabel.Trim(); s.ShowPortfolioLink=request.ShowPortfolioLink; s.PortfolioLabel=request.PortfolioLabel.Trim();
        s.ShowOrderLink=request.ShowOrderLink; s.OrderLabel=request.OrderLabel.Trim(); s.ShowAboutLink=request.ShowAboutLink; s.AboutLabel=request.AboutLabel.Trim();
        s.ShowContactLink=request.ShowContactLink; s.ContactLabel=request.ContactLabel.Trim(); s.UpdatedAt=DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken); return ToAdminBrand(s);
    }

    private async Task EnsureBrandImageAsync(Guid? id,string field,CancellationToken ct)
    { if(id is not null && !await dbContext.UploadedFiles.AsNoTracking().AnyAsync(x=>x.Id==id&&x.Purpose==UploadPurpose.BrandAsset&&x.ContentType.StartsWith("image/"),ct)) throw new BrandSettingsConflictException(field,"Select a valid uploaded brand image."); }

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
        CustomerConfirmationEmailsEnabled = false,
        CustomerOrderConfirmationSubject = SiteSettingsEntity.DefaultCustomerOrderConfirmationSubject,
        CustomerOrderConfirmationBody = SiteSettingsEntity.DefaultCustomerOrderConfirmationBody,
        CustomerContactConfirmationSubject = SiteSettingsEntity.DefaultCustomerContactConfirmationSubject,
        CustomerContactConfirmationBody = SiteSettingsEntity.DefaultCustomerContactConfirmationBody,
        EmailDeliveryProvider = "Configuration",
        EmailDeliverySenderName = "Bespoke Sewing Studio",
        LogoAltText = "Bespoke Sewing Studio logo", BrandDisplayName = "Bespoke Sewing Studio",
        HeaderCtaLabel = "Book Now", HeaderCtaUrl = "/order", DefaultMetaTitle = "Bespoke Sewing Studio",
        DefaultMetaDescription = "Bespoke sewing, tailoring, dressmaking, alterations and memory bears.",
        ServicesLabel = "Services", PortfolioLabel = "Portfolio", OrderLabel = "Order", AboutLabel = "About", ContactLabel = "Contact",
        ShowServicesLink = true, ShowPortfolioLink = true, ShowOrderLink = true, ShowAboutLink = true, ShowContactLink = true,
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
            settings.CustomerConfirmationEmailsEnabled,
            settings.CustomerOrderConfirmationSubject,
            settings.CustomerOrderConfirmationBody,
            settings.CustomerContactConfirmationSubject,
            settings.CustomerContactConfirmationBody,
            settings.FacebookUrl,
            settings.InstagramUrl,
            settings.TikTokUrl,
            settings.PinterestUrl,
            settings.FooterText,
            settings.ServiceAreaText,
            settings.BusinessLegalName,
            settings.UpdatedAt);

    private static BrandNavigationResponse Navigation(SiteSettingsEntity s)=>new(s.ShowServicesLink,s.ServicesLabel,s.ShowPortfolioLink,s.PortfolioLabel,s.ShowOrderLink,s.OrderLabel,s.ShowAboutLink,s.AboutLabel,s.ShowContactLink,s.ContactLabel);
    private static string? BrandUrl(Guid? id)=>id is null?null:$"/api/brand/images/{id}";
    private static PublicBrandSettingsResponse ToPublicBrand(SiteSettingsEntity s)=>new(s.BrandDisplayName,BrandUrl(s.LogoFileId),s.LogoAltText,BrandUrl(s.FaviconFileId),s.HeaderCtaLabel,s.HeaderCtaUrl,s.DefaultMetaTitle,s.DefaultMetaDescription,s.DefaultOgTitle,s.DefaultOgDescription,BrandUrl(s.DefaultOgImageFileId),Navigation(s));
    private static AdminBrandSettingsResponse ToAdminBrand(SiteSettingsEntity s)=>new(s.BrandDisplayName,BrandUrl(s.LogoFileId),s.LogoFileId,s.LogoAltText,BrandUrl(s.FaviconFileId),s.FaviconFileId,s.HeaderCtaLabel,s.HeaderCtaUrl,s.DefaultMetaTitle,s.DefaultMetaDescription,s.DefaultOgTitle,s.DefaultOgDescription,BrandUrl(s.DefaultOgImageFileId),s.DefaultOgImageFileId,Navigation(s),s.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    private static string? NormalizeEmail(string? value) =>
        Normalize(value)?.ToLowerInvariant();
}
