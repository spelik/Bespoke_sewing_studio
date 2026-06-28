namespace BespokeStudio.Domain.Entities;

public sealed class RepeatableContentItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string GroupKey { get; set; }
    public required string ItemKey { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Body { get; set; }
    public string? Label { get; set; }
    public string? Value { get; set; }
    public string? IconKey { get; set; }
    public string? Url { get; set; }
    public int? Rating { get; set; }
    public string? Location { get; set; }
    public string? Service { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ArchivedAt { get; set; }
}
