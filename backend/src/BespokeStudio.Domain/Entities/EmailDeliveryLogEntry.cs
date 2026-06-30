namespace BespokeStudio.Domain.Entities;

public sealed class EmailDeliveryLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string MessageType { get; set; }
    public required string RecipientEmail { get; set; }
    public required string Subject { get; set; }
    public required string Provider { get; set; }
    public required string Status { get; set; }
    public bool SentExternally { get; set; }
    public required string ResultMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityLabel { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
