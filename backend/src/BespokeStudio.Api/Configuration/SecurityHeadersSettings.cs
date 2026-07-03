namespace BespokeStudio.Api.Configuration;

public sealed class SecurityHeadersSettings
{
    public const string SectionName = "SecurityHeaders";

    public bool EnableContentSecurityPolicy { get; init; } = true;

    public string ContentSecurityPolicy { get; init; } =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "img-src 'self' data: blob:; " +
        "font-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self'; " +
        "connect-src 'self'";
}
