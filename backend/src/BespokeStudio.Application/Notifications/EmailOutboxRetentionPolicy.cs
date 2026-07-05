using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Notifications;

public static class EmailOutboxRetentionPolicy
{
    public static bool IsBodyPurgeEligible(
        EmailOutboxMessage message,
        DateTimeOffset now,
        int bodyRetentionDays,
        string purgedBodyPlaceholder)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(purgedBodyPlaceholder);

        if (message.Status is not (EmailOutboxStatus.Succeeded or EmailOutboxStatus.Skipped))
        {
            return false;
        }

        if (IsAlreadyPurged(message.HtmlBody, message.TextBody, purgedBodyPlaceholder))
        {
            return false;
        }

        var ageAnchor = ResolveAgeAnchor(message);
        return ageAnchor <= now.AddDays(-bodyRetentionDays);
    }

    public static bool IsMessageDeleteEligible(
        EmailOutboxMessage message,
        DateTimeOffset now,
        int messageRetentionDays)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Status is not (EmailOutboxStatus.Succeeded or EmailOutboxStatus.Skipped))
        {
            return false;
        }

        var ageAnchor = ResolveAgeAnchor(message);
        return ageAnchor <= now.AddDays(-messageRetentionDays);
    }

    public static bool IsBodyPurgeEligibleAfterDeletePass(
        EmailOutboxMessage message,
        DateTimeOffset now,
        int bodyRetentionDays,
        int messageRetentionDays,
        string purgedBodyPlaceholder)
    {
        if (!IsBodyPurgeEligible(message, now, bodyRetentionDays, purgedBodyPlaceholder))
        {
            return false;
        }

        return !IsMessageDeleteEligible(message, now, messageRetentionDays);
    }

    public static string ResolveSummaryMessage(
        int succeededBodyPurgeCandidateCount,
        int skippedBodyPurgeCandidateCount,
        int succeededDeleteCandidateCount,
        int skippedDeleteCandidateCount)
    {
        var totalCandidates = succeededBodyPurgeCandidateCount
            + skippedBodyPurgeCandidateCount
            + succeededDeleteCandidateCount
            + skippedDeleteCandidateCount;

        return totalCandidates == 0
            ? "No retention cleanup candidates."
            : "Email outbox retention cleanup candidates are available.";
    }

    public static DateTimeOffset ResolveAgeAnchor(EmailOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.Status == EmailOutboxStatus.Succeeded
            ? message.SentAt ?? message.UpdatedAt
            : message.UpdatedAt;
    }

    public static bool IsAlreadyPurged(
        string? htmlBody,
        string? textBody,
        string purgedBodyPlaceholder) =>
        htmlBody is null && string.Equals(textBody, purgedBodyPlaceholder, StringComparison.Ordinal);
}
