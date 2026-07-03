namespace BespokeStudio.Api.Configuration;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public int ForwardLimit { get; init; } = 1;
    public List<string> KnownProxies { get; init; } = [];
    public List<string> KnownNetworks { get; init; } = [];
}
