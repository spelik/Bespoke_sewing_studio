namespace BespokeStudio.Application.Contracts.Clients;

public sealed record ClientListItemResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    int OrdersCount,
    DateTimeOffset CreatedAt);
