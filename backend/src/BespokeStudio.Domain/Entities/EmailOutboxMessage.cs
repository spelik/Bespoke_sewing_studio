using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class EmailOutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string MessageType { get; set; }
    public required string RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public required string Subject { get; set; }
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastError { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityLabel { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? EmailDeliveryLogEntryId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
