namespace BespokeStudio.Application.Contracts.SiteSettings;

public sealed record BrandNavigationResponse(
    bool ShowServicesLink, string ServicesLabel,
    bool ShowPortfolioLink, string PortfolioLabel,
    bool ShowOrderLink, string OrderLabel,
    bool ShowAboutLink, string AboutLabel,
    bool ShowContactLink, string ContactLabel);

public sealed record PublicBrandSettingsResponse(
    string BrandDisplayName, string? LogoUrl, string LogoAltText, string? FaviconUrl,
    string HeaderCtaLabel, string HeaderCtaUrl, string DefaultMetaTitle,
    string DefaultMetaDescription, string? DefaultOgTitle, string? DefaultOgDescription,
    string? DefaultOgImageUrl, BrandNavigationResponse Navigation);

public sealed record AdminBrandSettingsResponse(
    string BrandDisplayName, string? LogoUrl, Guid? LogoFileId, string LogoAltText,
    string? FaviconUrl, Guid? FaviconFileId, string HeaderCtaLabel, string HeaderCtaUrl,
    string DefaultMetaTitle, string DefaultMetaDescription, string? DefaultOgTitle,
    string? DefaultOgDescription, string? DefaultOgImageUrl, Guid? DefaultOgImageFileId,
    BrandNavigationResponse Navigation, DateTimeOffset UpdatedAt);

public sealed record UpdateBrandSettingsRequest(
    string BrandDisplayName, Guid? LogoFileId, string LogoAltText, Guid? FaviconFileId,
    string HeaderCtaLabel, string HeaderCtaUrl, string DefaultMetaTitle,
    string DefaultMetaDescription, string? DefaultOgTitle, string? DefaultOgDescription,
    Guid? DefaultOgImageFileId, bool ShowServicesLink, string ServicesLabel,
    bool ShowPortfolioLink, string PortfolioLabel, bool ShowOrderLink, string OrderLabel,
    bool ShowAboutLink, string AboutLabel, bool ShowContactLink, string ContactLabel);
