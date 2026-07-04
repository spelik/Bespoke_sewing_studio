using BespokeStudio.Api.Versioning;

namespace BespokeStudio.Tests.Versioning;

public sealed class ApiVersionInfoProviderTests
{
    [Fact]
    public void GetVersionInfo_WithoutBuildVariables_UsesSafeFallbacks()
    {
        var startedAt = new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
        var provider = new ApiVersionInfoProvider(
            "Development",
            startedAt,
            getEnvironmentVariable: _ => null);

        var result = provider.GetVersionInfo();

        Assert.Equal("Bespoke Sewing Studio API", result.Application);
        Assert.Equal("0.1.0", result.Version);
        Assert.Equal("Development", result.Environment);
        Assert.StartsWith(".NET", result.Framework);
        Assert.Null(result.Commit);
        Assert.Null(result.BuildTime);
        Assert.Equal(startedAt, result.StartedAt);
    }

    [Fact]
    public void GetVersionInfo_WithBuildVariables_UsesConfiguredMetadata()
    {
        var variables = new Dictionary<string, string?>
        {
            ["BUILD_VERSION"] = "1.2.3",
            ["GIT_COMMIT"] = "abc123",
            ["BUILD_TIME"] = "2026-07-04T11:30:00Z"
        };
        var provider = new ApiVersionInfoProvider(
            "Production",
            DateTimeOffset.UtcNow,
            getEnvironmentVariable: name => variables.GetValueOrDefault(name));

        var result = provider.GetVersionInfo();

        Assert.Equal("1.2.3", result.Version);
        Assert.Equal("abc123", result.Commit);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 4, 11, 30, 0, TimeSpan.Zero),
            result.BuildTime);
    }

    [Fact]
    public void GetVersionInfo_InvalidBuildTime_DoesNotExposeRawValue()
    {
        var provider = new ApiVersionInfoProvider(
            "Production",
            DateTimeOffset.UtcNow,
            getEnvironmentVariable: name => name == "BUILD_TIME" ? "not-a-date" : null);

        var result = provider.GetVersionInfo();

        Assert.Null(result.BuildTime);
    }
}
