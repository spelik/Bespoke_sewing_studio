namespace BespokeStudio.Application.Notifications;

public static class EmailOutboxRetryPolicy
{
    private static readonly int[] RetryMultipliers = [1, 5, 15, 60];

    public static TimeSpan CalculateDelay(
        int attempts,
        int retryBaseSeconds,
        int retryMaxMinutes)
    {
        var normalizedAttempts = Math.Max(1, attempts);
        var multiplierIndex = Math.Min(normalizedAttempts - 1, RetryMultipliers.Length - 1);
        var seconds = (long)Math.Max(1, retryBaseSeconds) * RetryMultipliers[multiplierIndex];
        var maximumSeconds = (long)Math.Max(1, retryMaxMinutes) * 60;

        return TimeSpan.FromSeconds(Math.Min(seconds, maximumSeconds));
    }
}
