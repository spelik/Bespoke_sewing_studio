namespace BespokeStudio.Tests.Integration.PostgreSql;

public static class PostgreSqlIntegrationSettings
{
    public const string RunTestsEnvironmentVariable = "BESPOKESTUDIO_RUN_POSTGRES_INTEGRATION_TESTS";
    public const string AdminConnectionStringEnvironmentVariable = "BESPOKESTUDIO_POSTGRES_ADMIN_CONNECTION_STRING";

    public const string SkipReason =
        "PostgreSQL integration tests are opt-in. Set BESPOKESTUDIO_RUN_POSTGRES_INTEGRATION_TESTS=true and BESPOKESTUDIO_POSTGRES_ADMIN_CONNECTION_STRING.";

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(RunTestsEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static string? AdminConnectionString =>
        Environment.GetEnvironmentVariable(AdminConnectionStringEnvironmentVariable)?.Trim();

    public static bool ShouldRun =>
        IsEnabled && !string.IsNullOrWhiteSpace(AdminConnectionString);
}
