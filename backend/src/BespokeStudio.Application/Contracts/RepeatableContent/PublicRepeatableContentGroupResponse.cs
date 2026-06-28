namespace BespokeStudio.Application.Contracts.RepeatableContent;

public sealed record PublicRepeatableContentGroupResponse(
    string GroupKey,
    IReadOnlyList<PublicRepeatableContentItemResponse> Items);

public sealed record PublicRepeatableContentItemResponse(
    Guid Id,
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
    int DisplayOrder);
