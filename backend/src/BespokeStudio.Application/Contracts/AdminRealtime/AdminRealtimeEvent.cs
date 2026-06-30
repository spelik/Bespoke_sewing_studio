namespace BespokeStudio.Application.Contracts.AdminRealtime;

public sealed record AdminRealtimeEvent(
    string EventType,
    string Entity,
    Guid EntityId,
    string? ReferenceNumber,
    DateTimeOffset OccurredAt);
