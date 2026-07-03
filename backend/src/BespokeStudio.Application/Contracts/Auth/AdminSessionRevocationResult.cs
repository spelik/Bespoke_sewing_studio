namespace BespokeStudio.Application.Contracts.Auth;

public sealed record AdminSessionRevocationResult(
    Guid SessionId,
    bool Revoked,
    bool IsCurrent);
