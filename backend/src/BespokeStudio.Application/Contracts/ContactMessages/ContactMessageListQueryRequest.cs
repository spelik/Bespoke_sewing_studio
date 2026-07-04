using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.ContactMessages;

public sealed record ContactMessageListQueryRequest(
    int Page,
    int PageSize,
    string? Search,
    ContactMessageStatus? Status);
