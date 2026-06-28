namespace BespokeStudio.Application.Contracts.Content;

public sealed record PublicPageContentResponse(string PageKey, IReadOnlyList<PublicPageContentSectionResponse> Sections);
public sealed record PublicPageContentSectionResponse(Guid Id, string SectionKey, string? Title, string? Subtitle,
    string? Body, string? CtaLabel, string? CtaUrl, string? ImageUrl, string? ImageAltText, int DisplayOrder);
