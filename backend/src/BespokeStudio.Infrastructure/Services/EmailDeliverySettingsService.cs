using System.Security.Cryptography;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Validation;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiteSettingsEntity = BespokeStudio.Domain.Entities.SiteSettings;

namespace BespokeStudio.Infrastructure.Services;

public sealed class EmailDeliverySettingsService(
    BespokeStudioDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<EmailDeliverySettingsService> logger) : IEmailDeliverySettingsService
{
    private const string ProtectorPurpose = "BespokeStudio.EmailDeliverySettings.v1";
    private const string DefaultSenderName = "Bespoke Sewing Studio";

    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public async Task<AdminEmailDeliverySettingsResponse> GetAdminSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToAdminResponse(settings);
    }

    public async Task<AdminEmailDeliverySettingsResponse> UpdateAdminSettingsAsync(
        UpdateEmailDeliverySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = EmailDeliverySettingsValidator.Validate(request);
        if (errors.Count > 0)
        {
            throw new EmailDeliverySettingsValidationException(errors);
        }

        var provider = EmailDeliverySettingsValidator.NormalizeProvider(request.Provider)
            ?? EmailDeliverySettingsValidator.ConfigurationProvider;
        var settings = await GetOrCreateAsync(cancellationToken);
        var senderName = EmailDeliverySettingsValidator.Normalize(request.SenderName) ?? DefaultSenderName;
        var gmailAddress = EmailDeliverySettingsValidator.NormalizeEmail(request.GmailAddress);
        var appPassword = string.IsNullOrWhiteSpace(request.AppPassword)
            ? null
            : EmailDeliverySettingsValidator.NormalizeAppPassword(request.AppPassword);

        settings.EmailDeliveryProvider = provider;
        settings.EmailDeliverySenderName = senderName;

        if (provider == EmailDeliverySettingsValidator.GmailSmtpProvider)
        {
            settings.EmailDeliveryGmailAddress = gmailAddress;

            if (!string.IsNullOrWhiteSpace(appPassword))
            {
                settings.EmailDeliveryAppPasswordProtected = protector.Protect(appPassword);
            }
            else if (request.ClearAppPassword)
            {
                settings.EmailDeliveryAppPasswordProtected = null;
            }

            if (string.IsNullOrWhiteSpace(settings.EmailDeliveryAppPasswordProtected))
            {
                throw new EmailDeliverySettingsValidationException(new Dictionary<string, string[]>
                {
                    [nameof(request.AppPassword)] =
                        ["A Google App Password is required before Gmail SMTP can be used."]
                });
            }
        }
        else if (request.ClearAppPassword)
        {
            settings.EmailDeliveryAppPasswordProtected = null;
        }

        settings.EmailDeliveryUpdatedAt = DateTimeOffset.UtcNow;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(settings);
    }

    public async Task<ResolvedEmailDeliverySettings> GetResolvedSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        var provider = EmailDeliverySettingsValidator.NormalizeProvider(settings.EmailDeliveryProvider)
            ?? EmailDeliverySettingsValidator.ConfigurationProvider;

        if (provider != EmailDeliverySettingsValidator.GmailSmtpProvider)
        {
            return new ResolvedEmailDeliverySettings(
                EmailDeliverySettingsValidator.ConfigurationProvider,
                GmailAddress: null,
                SenderName: settings.EmailDeliverySenderName,
                AppPassword: null,
                AppPasswordConfigured: false,
                ConfigurationError: null);
        }

        var gmailAddress = EmailDeliverySettingsValidator.NormalizeEmail(settings.EmailDeliveryGmailAddress);
        if (string.IsNullOrWhiteSpace(gmailAddress))
        {
            return GmailConfigurationError(settings, "Gmail address is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.EmailDeliveryAppPasswordProtected))
        {
            return GmailConfigurationError(settings, "Gmail App Password is not configured.");
        }

        try
        {
            return new ResolvedEmailDeliverySettings(
                EmailDeliverySettingsValidator.GmailSmtpProvider,
                GmailAddress: gmailAddress,
                SenderName: settings.EmailDeliverySenderName,
                AppPassword: protector.Unprotect(settings.EmailDeliveryAppPasswordProtected),
                AppPasswordConfigured: true,
                ConfigurationError: null);
        }
        catch (CryptographicException exception)
        {
            logger.LogError(exception, "Stored Gmail SMTP App Password could not be decrypted.");
            return GmailConfigurationError(settings, "Stored Gmail SMTP App Password could not be decrypted.");
        }
    }

    private static ResolvedEmailDeliverySettings GmailConfigurationError(
        SiteSettingsEntity settings,
        string message) =>
        new(
            EmailDeliverySettingsValidator.GmailSmtpProvider,
            GmailAddress: settings.EmailDeliveryGmailAddress,
            SenderName: settings.EmailDeliverySenderName,
            AppPassword: null,
            AppPasswordConfigured: !string.IsNullOrWhiteSpace(settings.EmailDeliveryAppPasswordProtected),
            ConfigurationError: message);

    private async Task<SiteSettingsEntity> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.SiteSettings.SingleOrDefaultAsync(
            candidate => candidate.Id == SiteSettingsEntity.SingletonId,
            cancellationToken);

        if (settings is not null)
        {
            EnsureDefaults(settings);
            return settings;
        }

        settings = CreateDefault();
        dbContext.SiteSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static void EnsureDefaults(SiteSettingsEntity settings)
    {
        if (string.IsNullOrWhiteSpace(settings.EmailDeliveryProvider))
        {
            settings.EmailDeliveryProvider = EmailDeliverySettingsValidator.ConfigurationProvider;
        }

        if (string.IsNullOrWhiteSpace(settings.EmailDeliverySenderName))
        {
            settings.EmailDeliverySenderName = DefaultSenderName;
        }
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
        EmailDeliveryProvider = EmailDeliverySettingsValidator.ConfigurationProvider,
        EmailDeliverySenderName = DefaultSenderName,
        LogoAltText = "Bespoke Sewing Studio logo",
        BrandDisplayName = "Bespoke Sewing Studio",
        HeaderCtaLabel = "Book Now",
        HeaderCtaUrl = "/order",
        DefaultMetaTitle = "Bespoke Sewing Studio",
        DefaultMetaDescription = "Bespoke sewing, tailoring, dressmaking, alterations and memory bears.",
        ServicesLabel = "Services",
        PortfolioLabel = "Portfolio",
        OrderLabel = "Order",
        AboutLabel = "About",
        ContactLabel = "Contact",
        ShowServicesLink = true,
        ShowPortfolioLink = true,
        ShowOrderLink = true,
        ShowAboutLink = true,
        ShowContactLink = true,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static AdminEmailDeliverySettingsResponse ToAdminResponse(SiteSettingsEntity settings)
    {
        EnsureDefaults(settings);
        return new AdminEmailDeliverySettingsResponse(
            settings.EmailDeliveryProvider,
            settings.EmailDeliveryGmailAddress,
            settings.EmailDeliverySenderName,
            !string.IsNullOrWhiteSpace(settings.EmailDeliveryAppPasswordProtected),
            settings.EmailDeliveryUpdatedAt);
    }
}
