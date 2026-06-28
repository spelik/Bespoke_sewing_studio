namespace BespokeStudio.Application.Contracts.RepeatableContent;

public sealed record AdminRepeatableContentItemResponse(
    Guid Id,
    string GroupKey,
    string ItemKey,
    string? Title,
    string? Subtitle,
    string? Body,
    string? Label,
    string? Value,
    string? IconKey,
    string? Url,
    int? Rating,
    string? Location,
    string? Service,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);
