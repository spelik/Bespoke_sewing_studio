using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using BespokeStudio.Application.Contracts;

namespace BespokeStudio.Api.Versioning;

public sealed class ApiVersionInfoProvider
{
    private const string ApplicationName = "Bespoke Sewing Studio API";
    private const int MaxMetadataLength = 128;

    private readonly ApiVersionResponse _versionInfo;

    public ApiVersionInfoProvider(
        string environment,
        DateTimeOffset startedAt,
        Assembly? assembly = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        assembly ??= typeof(ApiVersionInfoProvider).Assembly;
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;

        var informationalVersion = GetSafeText(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        var assemblyVersion = assembly.GetName().Version?.ToString(3);
        var configuredVersion = GetSafeText(getEnvironmentVariable("BUILD_VERSION"));
        var configuredCommit = GetSafeText(getEnvironmentVariable("GIT_COMMIT"));
        var configuredBuildTime = GetSafeText(getEnvironmentVariable("BUILD_TIME"));

        _versionInfo = new ApiVersionResponse(
            Application: ApplicationName,
            Version: configuredVersion ?? informationalVersion ?? assemblyVersion ?? "unknown",
            Environment: GetSafeText(environment) ?? "Unknown",
            Framework: RuntimeInformation.FrameworkDescription,
            Commit: configuredCommit,
            BuildTime: ParseBuildTime(configuredBuildTime),
            StartedAt: startedAt.ToUniversalTime());
    }

    public ApiVersionResponse GetVersionInfo() => _versionInfo;

    private static DateTimeOffset? ParseBuildTime(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var buildTime)
            ? buildTime
            : null;

    private static string? GetSafeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = string.Concat(value.Trim().Where(character => !char.IsControl(character)));
        if (normalized.Length == 0)
        {
            return null;
        }

        return normalized.Length <= MaxMetadataLength
            ? normalized
            : normalized[..MaxMetadataLength];
    }
}
