using System.Data.Common;
using System.Text.RegularExpressions;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Tests.Integration.PostgreSql;

public sealed partial class PostgreSqlTestDatabase : IAsyncDisposable
{
    private static readonly Regex SafeDatabaseNamePattern = SafeDatabaseNameRegex();

    private readonly string _adminConnectionString;

    private PostgreSqlTestDatabase(
        string adminConnectionString,
        string databaseName,
        string appConnectionString)
    {
        _adminConnectionString = adminConnectionString;
        DatabaseName = databaseName;
        AppConnectionString = appConnectionString;
    }

    public string DatabaseName { get; }

    public string AppConnectionString { get; }

    public static async Task<PostgreSqlTestDatabase> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var adminConnectionString = PostgreSqlIntegrationSettings.AdminConnectionString
            ?? throw new InvalidOperationException(PostgreSqlIntegrationSettings.SkipReason);

        var databaseName = $"bespoke_studio_integration_{Guid.NewGuid():N}";
        if (!SafeDatabaseNamePattern.IsMatch(databaseName))
        {
            throw new InvalidOperationException("Generated PostgreSQL database name is invalid.");
        }

        await ExecuteAdminSqlAsync(
            adminConnectionString,
            $"""CREATE DATABASE "{databaseName}";""",
            cancellationToken);

        var appConnectionString = BuildAppConnectionString(adminConnectionString, databaseName);
        return new PostgreSqlTestDatabase(adminConnectionString, databaseName, appConnectionString);
    }

    public BespokeStudioDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseNpgsql(
                AppConnectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                    typeof(BespokeStudioDbContext).Assembly.FullName))
            .Options;

        return new BespokeStudioDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await ExecuteAdminSqlAsync(
                _adminConnectionString,
                $"""DROP DATABASE IF EXISTS "{DatabaseName}" WITH (FORCE);""",
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Failed to drop temporary PostgreSQL database '{DatabaseName}'. " +
                "Drop it manually if it remains. Connection strings are not logged.",
                exception);
        }
    }

    private static string BuildAppConnectionString(string adminConnectionString, string databaseName)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = adminConnectionString
        };
        builder["Database"] = databaseName;
        return builder.ConnectionString;
    }

    private static async Task ExecuteAdminSqlAsync(
        string adminConnectionString,
        string sql,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseNpgsql(adminConnectionString)
            .Options;

        await using var context = new BespokeStudioDbContext(options);
        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    [GeneratedRegex("^[a-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeDatabaseNameRegex();
}
