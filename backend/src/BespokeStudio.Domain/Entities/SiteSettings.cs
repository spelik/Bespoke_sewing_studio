namespace BespokeStudio.Domain.Entities;

public sealed class SiteSettings
{
    public static readonly Guid SingletonId = new("7e7a43ab-bd37-4e9f-8e62-d384e8663180");

    public Guid Id { get; init; } = SingletonId;
    public required string StudioName { get; set; }
    public string? SiteTagline { get; set; }
    public string? PublicEmail { get; set; }
    public string? PublicPhone { get; set; }
    public string? WhatsAppPhone { get; set; }
    public string? ContactButtonLabel { get; set; }
    public string? ContactIntroText { get; set; }
    public string? NotificationEmail { get; set; }
    public string? NotificationPhone { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool WhatsAppNotificationsEnabled { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TikTokUrl { get; set; }
    public string? PinterestUrl { get; set; }
    public string? FooterText { get; set; }
    public string? ServiceAreaText { get; set; }
    public string? BusinessLegalName { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
