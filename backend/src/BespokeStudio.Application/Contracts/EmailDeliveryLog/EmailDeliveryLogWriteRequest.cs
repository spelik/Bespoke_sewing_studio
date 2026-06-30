namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailDeliveryLogWriteRequest(
    string MessageType,
    string RecipientEmail,
    string Subject,
    string Provider,
    string Status,
    bool SentExternally,
    string ResultMessage,
    string? ErrorMessage,
    string? RelatedEntityType,
    string? RelatedEntityId,
    string? RelatedEntityLabel,
    DateTimeOffset? CompletedAt);
