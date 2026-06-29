namespace BespokeStudio.Domain.Entities;

public sealed class SiteSettings
{
    public static readonly Guid SingletonId = new("7e7a43ab-bd37-4e9f-8e62-d384e8663180");

    public const string DefaultCustomerOrderConfirmationSubject = "We received your order request";
    public const string DefaultCustomerOrderConfirmationBody = "Hello {{customerName}},\n\nThank you for your order request to {{studioName}}.\nWe have received your details and will review them shortly.\n\nKind regards,\n{{studioName}}";
    public const string DefaultCustomerContactConfirmationSubject = "We received your message";
    public const string DefaultCustomerContactConfirmationBody = "Hello {{customerName}},\n\nThank you for contacting {{studioName}}.\nWe have received your message and will review it shortly.\n\nKind regards,\n{{studioName}}";

    public Guid Id { get; init; } = SingletonId;
    public required string StudioName { get; set; }
    public string? SiteTagline { get; set; }
    public string? PublicEmail { get; set; }
    public string? PublicPhone { get; set; }
    public string? ContactButtonLabel { get; set; }
    public string? ContactIntroText { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool CustomerConfirmationEmailsEnabled { get; set; }
    public string CustomerOrderConfirmationSubject { get; set; } = DefaultCustomerOrderConfirmationSubject;
    public string CustomerOrderConfirmationBody { get; set; } = DefaultCustomerOrderConfirmationBody;
    public string CustomerContactConfirmationSubject { get; set; } = DefaultCustomerContactConfirmationSubject;
    public string CustomerContactConfirmationBody { get; set; } = DefaultCustomerContactConfirmationBody;
    public string EmailDeliveryProvider { get; set; } = "Configuration";
    public string? EmailDeliveryGmailAddress { get; set; }
    public string? EmailDeliveryAppPasswordProtected { get; set; }
    public string EmailDeliverySenderName { get; set; } = "Bespoke Sewing Studio";
    public DateTimeOffset? EmailDeliveryUpdatedAt { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TikTokUrl { get; set; }
    public string? PinterestUrl { get; set; }
    public string? FooterText { get; set; }
    public string? ServiceAreaText { get; set; }
    public string? BusinessLegalName { get; set; }
    public Guid? LogoFileId { get; set; }
    public Guid? FaviconFileId { get; set; }
    public Guid? DefaultOgImageFileId { get; set; }
    public required string LogoAltText { get; set; }
    public required string BrandDisplayName { get; set; }
    public required string HeaderCtaLabel { get; set; }
    public required string HeaderCtaUrl { get; set; }
    public required string DefaultMetaTitle { get; set; }
    public required string DefaultMetaDescription { get; set; }
    public string? DefaultOgTitle { get; set; }
    public string? DefaultOgDescription { get; set; }
    public bool ShowServicesLink { get; set; } = true;
    public bool ShowPortfolioLink { get; set; } = true;
    public bool ShowOrderLink { get; set; } = true;
    public bool ShowAboutLink { get; set; } = true;
    public bool ShowContactLink { get; set; } = true;
    public required string ServicesLabel { get; set; }
    public required string PortfolioLabel { get; set; }
    public required string OrderLabel { get; set; }
    public required string AboutLabel { get; set; }
    public required string ContactLabel { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
