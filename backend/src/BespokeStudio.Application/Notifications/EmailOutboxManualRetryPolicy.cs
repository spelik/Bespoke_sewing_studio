using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Notifications;

public static class EmailOutboxManualRetryPolicy
{
    public static bool IsManualRetryEligible(EmailOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.Status == EmailOutboxStatus.Failed
            && message.Attempts >= message.MaxAttempts
            && message.NextAttemptAt is null;
    }
}
