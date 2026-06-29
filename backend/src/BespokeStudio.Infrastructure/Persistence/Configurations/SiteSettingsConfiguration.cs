using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    private static readonly DateTimeOffset InitialUpdatedAt =
        new(2026, 6, 27, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("SiteSettings", table =>
            table.HasCheckConstraint(
                "CK_SiteSettings_Singleton",
                $"\"Id\" = '{SiteSettings.SingletonId}'::uuid"));

        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.StudioName).HasMaxLength(150).IsRequired();
        builder.Property(settings => settings.SiteTagline).HasMaxLength(500);
        builder.Property(settings => settings.PublicEmail).HasMaxLength(320);
        builder.Property(settings => settings.PublicPhone).HasMaxLength(50);
        builder.Property(settings => settings.ContactButtonLabel).HasMaxLength(100);
        builder.Property(settings => settings.ContactIntroText).HasMaxLength(1000);
        builder.Property(settings => settings.EmailDeliveryProvider).HasMaxLength(40).IsRequired();
        builder.Property(settings => settings.EmailDeliveryGmailAddress).HasMaxLength(320);
        builder.Property(settings => settings.EmailDeliveryAppPasswordProtected).HasMaxLength(4096);
        builder.Property(settings => settings.EmailDeliverySenderName).HasMaxLength(150).IsRequired();
        builder.Property(settings => settings.FacebookUrl).HasMaxLength(2048);
        builder.Property(settings => settings.InstagramUrl).HasMaxLength(2048);
        builder.Property(settings => settings.TikTokUrl).HasMaxLength(2048);
        builder.Property(settings => settings.PinterestUrl).HasMaxLength(2048);
        builder.Property(settings => settings.FooterText).HasMaxLength(500);
        builder.Property(settings => settings.ServiceAreaText).HasMaxLength(500);
        builder.Property(settings => settings.BusinessLegalName).HasMaxLength(200);
        builder.Property(x => x.LogoAltText).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BrandDisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.HeaderCtaLabel).HasMaxLength(100).IsRequired();
        builder.Property(x => x.HeaderCtaUrl).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.DefaultMetaTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DefaultMetaDescription).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DefaultOgTitle).HasMaxLength(200);
        builder.Property(x => x.DefaultOgDescription).HasMaxLength(500);
        builder.Property(x => x.ServicesLabel).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PortfolioLabel).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OrderLabel).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AboutLabel).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContactLabel).HasMaxLength(50).IsRequired();
        builder.HasOne<UploadedFileMetadata>().WithMany().HasForeignKey(x => x.LogoFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UploadedFileMetadata>().WithMany().HasForeignKey(x => x.FaviconFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UploadedFileMetadata>().WithMany().HasForeignKey(x => x.DefaultOgImageFileId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(settings => settings.UpdatedAt).IsRequired();

        builder.HasData(new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            StudioName = "Bespoke Sewing Studio",
            SiteTagline = "Bespoke sewing, tailoring, dressmaking, alterations and memory bears.",
            PublicPhone = "074 6734 7194",
            ContactButtonLabel = "Send Enquiry",
            ContactIntroText = "Consultations and orders are arranged individually.",
            ServiceAreaText = "Appointments arranged individually.",
            EmailNotificationsEnabled = false,
            EmailDeliveryProvider = "Configuration",
            EmailDeliverySenderName = "Bespoke Sewing Studio",
            FooterText = "Bespoke Sewing Studio. All rights reserved.",
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
            UpdatedAt = InitialUpdatedAt
        });
    }
}
