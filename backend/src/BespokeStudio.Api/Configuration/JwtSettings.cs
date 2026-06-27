namespace BespokeStudio.Api.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "BespokeStudio.Api";
    public string Audience { get; init; } = "BespokeStudio.Admin";
    public string SigningKey { get; init; } = string.Empty;
    public int ExpirationHours { get; init; } = 4;
}
