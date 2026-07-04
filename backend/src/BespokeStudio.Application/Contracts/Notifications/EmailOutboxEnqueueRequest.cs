namespace BespokeStudio.Application.Contracts.Notifications;

public sealed record EmailOutboxEnqueueRequest(
    string MessageType,
    string RecipientEmail,
    string? RecipientName,
    string Subject,
    string? HtmlBody,
    string? TextBody,
    string? RelatedEntityType,
    string? RelatedEntityId,
    string? RelatedEntityLabel,
    string? CorrelationId = null);
