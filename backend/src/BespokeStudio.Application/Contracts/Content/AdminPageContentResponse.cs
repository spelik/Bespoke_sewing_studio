namespace BespokeStudio.Application.Contracts.Content;

public sealed record AdminPageContentResponse(Guid Id, string PageKey, string SectionKey, string? Title, string? Subtitle,
    string? Body, string? CtaLabel, string? CtaUrl, Guid? ImageFileId, string? ImageUrl, string? ImageAltText,
    int DisplayOrder, bool IsActive, DateTimeOffset UpdatedAt, DateTimeOffset? ArchivedAt);
