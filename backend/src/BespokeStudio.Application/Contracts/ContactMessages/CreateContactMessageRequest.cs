namespace BespokeStudio.Application.Contracts.ContactMessages;

public sealed record CreateContactMessageRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Subject,
    string Message,
    bool Consent,
    string? WebsiteUrl,
    DateTimeOffset? FormLoadedAt);
