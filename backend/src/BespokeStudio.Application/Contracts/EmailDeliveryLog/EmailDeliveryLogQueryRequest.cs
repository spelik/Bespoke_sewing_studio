namespace BespokeStudio.Application.Contracts.EmailDeliveryLog;

public sealed record EmailDeliveryLogQueryRequest(
    int Take,
    string? Search,
    string? MessageType,
    string? Status,
    string? RecipientEmail,
    string? Provider);
