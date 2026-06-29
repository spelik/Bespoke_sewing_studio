using System.Data;
using System.Globalization;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

internal static class RequestReferenceNumberGenerator
{
    public static async Task<string> CreateAsync(
        BespokeStudioDbContext dbContext,
        string sequenceName,
        string prefix,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sequenceValue = await GetNextSequenceValueAsync(dbContext, sequenceName, cancellationToken);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix}-{now.Year}-{sequenceValue:000000}");
    }

    private static async Task<long> GetNextSequenceValueAsync(
        BespokeStudioDbContext dbContext,
        string sequenceName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;

        if (shouldCloseConnection)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT nextval('\"{sequenceName}\"')";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
