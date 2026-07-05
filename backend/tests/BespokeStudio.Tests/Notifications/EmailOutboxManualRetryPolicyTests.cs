using BespokeStudio.Application.Notifications;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Tests.Notifications;

public sealed class EmailOutboxManualRetryPolicyTests
{
    [Fact]
    public void IsManualRetryEligible_ExhaustedFailedWithoutNextAttempt_ReturnsTrue()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Failed,
            attempts: 5,
            maxAttempts: 5,
            nextAttemptAt: null);

        Assert.True(EmailOutboxManualRetryPolicy.IsManualRetryEligible(message));
    }

    [Fact]
    public void IsManualRetryEligible_FailedWithScheduledNextAttempt_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Failed,
            attempts: 5,
            maxAttempts: 5,
            nextAttemptAt: DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.False(EmailOutboxManualRetryPolicy.IsManualRetryEligible(message));
    }

    [Fact]
    public void IsManualRetryEligible_FailedWithAttemptsBelowMax_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Failed,
            attempts: 2,
            maxAttempts: 5,
            nextAttemptAt: null);

        Assert.False(EmailOutboxManualRetryPolicy.IsManualRetryEligible(message));
    }

    [Theory]
    [InlineData(EmailOutboxStatus.Pending)]
    [InlineData(EmailOutboxStatus.Processing)]
    [InlineData(EmailOutboxStatus.Succeeded)]
    [InlineData(EmailOutboxStatus.Skipped)]
    public void IsManualRetryEligible_NonFailedStatuses_ReturnFalse(EmailOutboxStatus status)
    {
        var message = CreateMessage(
            status: status,
            attempts: 5,
            maxAttempts: 5,
            nextAttemptAt: null);

        Assert.False(EmailOutboxManualRetryPolicy.IsManualRetryEligible(message));
    }

    private static EmailOutboxMessage CreateMessage(
        EmailOutboxStatus status,
        int attempts,
        int maxAttempts,
        DateTimeOffset? nextAttemptAt) => new()
        {
            MessageType = "test_email",
            RecipientEmail = "recipient@example.com",
            Subject = "Test",
            Status = status,
            Attempts = attempts,
            MaxAttempts = maxAttempts,
            NextAttemptAt = nextAttemptAt
        };
}
