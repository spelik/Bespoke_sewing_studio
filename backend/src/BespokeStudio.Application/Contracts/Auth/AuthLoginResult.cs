namespace BespokeStudio.Application.Contracts.Auth;

public sealed record AuthLoginResult(
    AuthSessionResult? Session,
    Guid? TwoFactorUserId)
{
    public bool RequiresTwoFactor => TwoFactorUserId.HasValue;
}
