using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.ContactMessages;

public sealed record ContactMessageListItemResponse(
    Guid Id,
    string ReferenceNumber,
    string FullName,
    string Email,
    string? Phone,
    string? Subject,
    string MessagePreview,
    ContactMessageStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
