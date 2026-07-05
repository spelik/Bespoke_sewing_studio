using BespokeStudio.Application.Notifications;

namespace BespokeStudio.Tests.Notifications;

public sealed class EmailOutboxMonitoringPolicyTests
{
    [Fact]
    public void ResolveHealthStatus_NoIssues_ReturnsHealthy()
    {
        var status = EmailOutboxMonitoringPolicy.ResolveHealthStatus(
            exhaustedFailedCount: 0,
            stalePendingCount: 0,
            failedCount: 0,
            retryingCount: 0);

        Assert.Equal(EmailOutboxMonitoringPolicy.Healthy, status);
    }

    [Fact]
    public void ResolveHealthStatus_ExhaustedFailed_ReturnsCritical()
    {
        var status = EmailOutboxMonitoringPolicy.ResolveHealthStatus(
            exhaustedFailedCount: 1,
            stalePendingCount: 0,
            failedCount: 1,
            retryingCount: 0);

        Assert.Equal(EmailOutboxMonitoringPolicy.Critical, status);
    }

    [Fact]
    public void ResolveHealthStatus_RetryingOnly_ReturnsWarning()
    {
        var status = EmailOutboxMonitoringPolicy.ResolveHealthStatus(
            exhaustedFailedCount: 0,
            stalePendingCount: 0,
            failedCount: 1,
            retryingCount: 1);

        Assert.Equal(EmailOutboxMonitoringPolicy.Warning, status);
    }

    [Fact]
    public void ResolveHealthStatus_StalePending_ReturnsWarning()
    {
        var status = EmailOutboxMonitoringPolicy.ResolveHealthStatus(
            exhaustedFailedCount: 0,
            stalePendingCount: 2,
            failedCount: 0,
            retryingCount: 0);

        Assert.Equal(EmailOutboxMonitoringPolicy.Warning, status);
    }

    [Fact]
    public void ResolveHealthStatus_FailedNotExhausted_ReturnsWarning()
    {
        var status = EmailOutboxMonitoringPolicy.ResolveHealthStatus(
            exhaustedFailedCount: 0,
            stalePendingCount: 0,
            failedCount: 3,
            retryingCount: 0);

        Assert.Equal(EmailOutboxMonitoringPolicy.Warning, status);
    }

    [Theory]
    [InlineData("Healthy", "Email outbox is healthy.")]
    [InlineData("Warning", "Email delivery has scheduled retries or stale pending messages.")]
    [InlineData("Critical", "Email delivery has failed messages that need review.")]
    public void ResolveSummaryMessage_ReturnsSafeMessage(string healthStatus, string expected)
    {
        Assert.Equal(expected, EmailOutboxMonitoringPolicy.ResolveSummaryMessage(healthStatus));
    }
}
