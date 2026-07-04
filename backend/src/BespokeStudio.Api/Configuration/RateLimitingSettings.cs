namespace BespokeStudio.Api.Configuration;

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int PublicUploadPermitLimit { get; init; } = 10;
    public int PublicOrderPermitLimit { get; init; } = 5;
    public int PublicContactPermitLimit { get; init; } = 5;
    public int WindowMinutes { get; init; } = 10;
    public int AuthLoginPermitLimit { get; init; } = 10;
    public int AuthLoginWindowMinutes { get; init; } = 15;
    public int AuthTwoFactorPermitLimit { get; init; } = 10;
    public int AuthTwoFactorWindowMinutes { get; init; } = 15;
}

public static class RateLimitPolicies
{
    public const string PublicUpload = "PublicUpload";
    public const string PublicOrder = "PublicOrder";
    public const string PublicContact = "PublicContact";
    public const string AuthLogin = "AuthLogin";
    public const string AuthTwoFactor = "AuthTwoFactor";
}
