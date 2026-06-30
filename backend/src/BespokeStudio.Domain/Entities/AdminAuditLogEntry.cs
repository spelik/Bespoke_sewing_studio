namespace BespokeStudio.Domain.Entities;

public sealed class AdminAuditLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ActorUserId { get; set; }
    public required string ActorEmail { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? EntityLabel { get; set; }
    public required string Summary { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
