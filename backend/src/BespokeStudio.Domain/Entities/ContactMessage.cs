using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Domain.Entities;

public sealed class ContactMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public required string Message { get; set; }
    public ContactMessageStatus Status { get; set; } = ContactMessageStatus.New;
    public bool ConsentGiven { get; set; }
    public DateTimeOffset? ConsentRecordedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
