namespace BespokeStudio.Application.Contracts.Auth;

public sealed record AuthTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    CurrentUserResponse User);
