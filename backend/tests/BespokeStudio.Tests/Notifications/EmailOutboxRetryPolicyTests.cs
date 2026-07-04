using BespokeStudio.Application.Notifications;

namespace BespokeStudio.Tests.Notifications;

public sealed class EmailOutboxRetryPolicyTests
{
    [Theory]
    [InlineData(1, 60)]
    [InlineData(2, 300)]
    [InlineData(3, 900)]
    [InlineData(4, 3600)]
    [InlineData(5, 3600)]
    public void CalculateDelay_UsesExpectedRetrySchedule(
        int attempts,
        int expectedSeconds)
    {
        var delay = EmailOutboxRetryPolicy.CalculateDelay(
            attempts,
            retryBaseSeconds: 60,
            retryMaxMinutes: 60);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void CalculateDelay_RespectsConfiguredMaximum()
    {
        var delay = EmailOutboxRetryPolicy.CalculateDelay(
            attempts: 4,
            retryBaseSeconds: 60,
            retryMaxMinutes: 10);

        Assert.Equal(TimeSpan.FromMinutes(10), delay);
    }
}
