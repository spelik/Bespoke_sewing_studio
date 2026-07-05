namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailDeliveryManualRetryResponse(
    Guid EmailDeliveryLogEntryId,
    Guid OutboxMessageId,
    string Status,
    string ResultMessage,
    string MessageType,
    string? RelatedEntityLabel,
    DateTimeOffset QueuedAt);
