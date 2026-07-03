namespace BespokeStudio.Application.Contracts.Auth;

public sealed record AdminSessionResponse(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? LastUsedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool IsCurrent,
    bool IsRevoked,
    string? UserAgent,
    string? CreatedByIp,
    string? RevocationReason,
    string Status);
