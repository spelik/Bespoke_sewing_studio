namespace BespokeStudio.Api.Configuration;

public sealed class CorsSettings
{
    public const string PolicyName = "FrontendDevelopment";
    public const string SectionName = "Cors";

    public List<string> AllowedOrigins { get; init; } = [];
}
