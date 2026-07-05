namespace BespokeStudio.Application.Notifications;

public static class EmailOutboxMonitoringPolicy
{
    public const string Healthy = "Healthy";
    public const string Warning = "Warning";
    public const string Critical = "Critical";

    public static string ResolveHealthStatus(
        int exhaustedFailedCount,
        int stalePendingCount,
        int failedCount,
        int retryingCount)
    {
        if (exhaustedFailedCount > 0)
        {
            return Critical;
        }

        if (stalePendingCount > 0 || failedCount > 0 || retryingCount > 0)
        {
            return Warning;
        }

        return Healthy;
    }

    public static string ResolveSummaryMessage(string healthStatus) => healthStatus switch
    {
        Critical => "Email delivery has failed messages that need review.",
        Warning => "Email delivery has scheduled retries or stale pending messages.",
        _ => "Email outbox is healthy."
    };
}
