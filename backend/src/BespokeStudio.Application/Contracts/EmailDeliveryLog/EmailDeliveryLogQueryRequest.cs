namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailDeliveryLogQueryRequest(
    int Page,
    int PageSize,
    string? Search,
    string? MessageType,
    string? Status,
    string? RecipientEmail,
    string? Provider);
