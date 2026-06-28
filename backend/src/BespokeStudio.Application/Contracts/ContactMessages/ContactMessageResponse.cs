using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.ContactMessages;

public sealed record ContactMessageResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Subject,
    string Message,
    ContactMessageStatus Status,
    bool ConsentGiven,
    DateTimeOffset? ConsentRecordedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
