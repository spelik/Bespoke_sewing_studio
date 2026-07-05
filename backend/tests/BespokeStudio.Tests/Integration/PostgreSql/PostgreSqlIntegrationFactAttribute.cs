namespace BespokeStudio.Tests.Integration.PostgreSql;

public sealed class PostgreSqlIntegrationFactAttribute : FactAttribute
{
    public PostgreSqlIntegrationFactAttribute()
    {
        if (!PostgreSqlIntegrationSettings.ShouldRun)
        {
            Skip = PostgreSqlIntegrationSettings.SkipReason;
        }
    }
}
