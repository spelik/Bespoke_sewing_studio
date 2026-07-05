namespace BespokeStudio.Application.Notifications;

public sealed class EmailOutboxMessageNotFoundException(Guid emailDeliveryLogEntryId)
    : Exception("The email log entry could not be found for manual retry.")
{
    public Guid EmailDeliveryLogEntryId { get; } = emailDeliveryLogEntryId;
}

public sealed class EmailManualRetryNotAllowedException(Guid emailDeliveryLogEntryId)
    : Exception("This email is not eligible for manual retry.")
{
    public Guid EmailDeliveryLogEntryId { get; } = emailDeliveryLogEntryId;
}
