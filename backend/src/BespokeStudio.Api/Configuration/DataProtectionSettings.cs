namespace BespokeStudio.Api.Configuration;

public sealed class DataProtectionSettings
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; init; } = "BespokeSewingStudio";
    public string KeysPath { get; init; } = string.Empty;
}
