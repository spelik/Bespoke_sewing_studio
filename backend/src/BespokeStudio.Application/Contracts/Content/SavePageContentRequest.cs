namespace BespokeStudio.Application.Contracts.Content;

public sealed record SavePageContentRequest(string PageKey, string SectionKey, string? Title, string? Subtitle,
    string? Body, string? CtaLabel, string? CtaUrl, Guid? ImageFileId, string? ImageAltText,
    int DisplayOrder, bool IsActive);
