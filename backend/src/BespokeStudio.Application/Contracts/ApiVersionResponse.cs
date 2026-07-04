namespace BespokeStudio.Application.Contracts;

public sealed record ApiVersionResponse(
    string Application,
    string Version,
    string Environment,
    string Framework,
    string? Commit,
    DateTimeOffset? BuildTime,
    DateTimeOffset StartedAt);
