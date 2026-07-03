namespace BespokeStudio.Application.Contracts.Auth;

public sealed record AuthSessionResult(
    AuthTokenResponse Token,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
