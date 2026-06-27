namespace BespokeStudio.Application.Contracts.Clients;

public sealed record ClientResponse(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
