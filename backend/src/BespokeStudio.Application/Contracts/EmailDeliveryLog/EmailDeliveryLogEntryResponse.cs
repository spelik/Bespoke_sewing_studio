namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailDeliveryLogEntryResponse(
    Guid Id,
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
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
