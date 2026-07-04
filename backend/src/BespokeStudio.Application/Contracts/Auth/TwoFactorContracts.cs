namespace BespokeStudio.Application.Contracts.Auth;

public sealed record TwoFactorRequiredResponse(bool RequiresTwoFactor = true);

public sealed record TwoFactorCodeRequest(string Code);

public sealed record TwoFactorPasswordRequest(string CurrentPassword);

public sealed record TwoFactorStatusResponse(
    bool IsEnabled,
    bool HasAuthenticatorKey,
    int RecoveryCodesRemaining);

public sealed record TwoFactorSetupResponse(
    string SharedKey,
    string AuthenticatorUri,
    string Issuer,
    string AccountName,
    AuthTokenResponse Token);

public sealed record TwoFactorEnableResponse(
    TwoFactorStatusResponse Status,
    IReadOnlyList<string> RecoveryCodes,
    AuthTokenResponse Token);

public sealed record TwoFactorStatusUpdateResponse(
    TwoFactorStatusResponse Status,
    AuthTokenResponse Token);

public sealed record TwoFactorRecoveryCodesResponse(
    IReadOnlyList<string> RecoveryCodes,
    int RecoveryCodesRemaining,
    AuthTokenResponse Token);
