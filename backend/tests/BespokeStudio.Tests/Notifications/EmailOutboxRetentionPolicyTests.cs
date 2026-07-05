using BespokeStudio.Application.Notifications;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Tests.Notifications;

public sealed class EmailOutboxRetentionPolicyTests
{
    private const string Placeholder = "[Email body purged by retention policy.]";
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsBodyPurgeEligible_OldSucceededWithRealBody_ReturnsTrue()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Succeeded,
            sentAt: Now.AddDays(-45),
            updatedAt: Now.AddDays(-45),
            htmlBody: "<p>Hello</p>",
            textBody: "Hello");

        Assert.True(EmailOutboxRetentionPolicy.IsBodyPurgeEligible(message, Now, 30, Placeholder));
    }

    [Fact]
    public void IsBodyPurgeEligible_AlreadyPurgedSucceeded_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Succeeded,
            sentAt: Now.AddDays(-45),
            updatedAt: Now.AddDays(-45),
            htmlBody: null,
            textBody: Placeholder);

        Assert.False(EmailOutboxRetentionPolicy.IsBodyPurgeEligible(message, Now, 30, Placeholder));
    }

    [Fact]
    public void IsBodyPurgeEligible_RecentSucceeded_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Succeeded,
            sentAt: Now.AddDays(-5),
            updatedAt: Now.AddDays(-5),
            htmlBody: "<p>Hello</p>",
            textBody: "Hello");

        Assert.False(EmailOutboxRetentionPolicy.IsBodyPurgeEligible(message, Now, 30, Placeholder));
    }

    [Fact]
    public void IsBodyPurgeEligible_OldSkipped_ReturnsTrue()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Skipped,
            sentAt: null,
            updatedAt: Now.AddDays(-45),
            htmlBody: null,
            textBody: "Skipped body");

        Assert.True(EmailOutboxRetentionPolicy.IsBodyPurgeEligible(message, Now, 30, Placeholder));
    }

    [Fact]
    public void IsBodyPurgeEligible_FailedExhausted_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Failed,
            sentAt: null,
            updatedAt: Now.AddDays(-120),
            htmlBody: "<p>Failed</p>",
            textBody: "Failed");

        Assert.False(EmailOutboxRetentionPolicy.IsBodyPurgeEligible(message, Now, 30, Placeholder));
    }

    [Theory]
    [InlineData(EmailOutboxStatus.Pending)]
    [InlineData(EmailOutboxStatus.Processing)]
    public void IsBodyPurgeEligible_NonTerminalStatuses_ReturnFalse(EmailOutboxStatus status)
    {
        var message = CreateMessage(
            status: status,
            sentAt: null,
            updatedAt: Now.AddDays(-120),
            htmlBody: "<p>Body</p>",
            textBody: "Body");

        Assert.False(EmailOutboxRetentionPolicy.IsBodyPurgeEligible(message, Now, 30, Placeholder));
    }

    [Fact]
    public void IsBodyPurgeEligible_RetryingFailed_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Failed,
            sentAt: null,
            updatedAt: Now.AddDays(-10),
            htmlBody: "<p>Retry</p>",
            textBody: "Retry");
        message.NextAttemptAt = Now.AddMinutes(15);

        Assert.False(EmailOutboxRetentionPolicy.IsBodyPurgeEligible(message, Now, 30, Placeholder));
    }

    [Fact]
    public void IsMessageDeleteEligible_OldSucceededBeyondDeleteRetention_ReturnsTrue()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Succeeded,
            sentAt: Now.AddDays(-100),
            updatedAt: Now.AddDays(-100),
            htmlBody: "<p>Hello</p>",
            textBody: "Hello");

        Assert.True(EmailOutboxRetentionPolicy.IsMessageDeleteEligible(message, Now, 90));
    }

    [Fact]
    public void IsMessageDeleteEligible_FailedExhaustedBeyondDeleteRetention_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Failed,
            sentAt: null,
            updatedAt: Now.AddDays(-200),
            htmlBody: "<p>Failed</p>",
            textBody: "Failed");

        Assert.False(EmailOutboxRetentionPolicy.IsMessageDeleteEligible(message, Now, 90));
    }

    [Fact]
    public void ResolveSummaryMessage_NoCandidates_ReturnsNoCandidatesMessage()
    {
        Assert.Equal(
            "No retention cleanup candidates.",
            EmailOutboxRetentionPolicy.ResolveSummaryMessage(0, 0, 0, 0));
    }

    [Fact]
    public void ResolveSummaryMessage_WithCandidates_ReturnsCandidatesMessage()
    {
        Assert.Equal(
            "Email outbox retention cleanup candidates are available.",
            EmailOutboxRetentionPolicy.ResolveSummaryMessage(1, 0, 0, 0));
    }

    [Fact]
    public void IsBodyPurgeEligibleAfterDeletePass_VeryOldSucceeded_ReturnsFalse()
    {
        var message = CreateMessage(
            status: EmailOutboxStatus.Succeeded,
            sentAt: Now.AddDays(-100),
            updatedAt: Now.AddDays(-100),
            htmlBody: "<p>Hello</p>",
            textBody: "Hello");

        Assert.False(EmailOutboxRetentionPolicy.IsBodyPurgeEligibleAfterDeletePass(
            message,
            Now,
            bodyRetentionDays: 30,
            messageRetentionDays: 90,
            Placeholder));
    }

    private static EmailOutboxMessage CreateMessage(
        EmailOutboxStatus status,
        DateTimeOffset? sentAt,
        DateTimeOffset updatedAt,
        string? htmlBody,
        string? textBody) => new()
        {
            MessageType = "test_email",
            RecipientEmail = "recipient@example.com",
            Subject = "Test",
            Status = status,
            SentAt = sentAt,
            UpdatedAt = updatedAt,
            HtmlBody = htmlBody,
            TextBody = textBody
        };
}
