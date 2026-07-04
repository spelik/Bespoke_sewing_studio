using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Auth;

namespace BespokeStudio.Application.Abstractions;

public interface IAuthService
{
    Task<AuthLoginResult?> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthSessionResult?> VerifyTwoFactorAsync(
        Guid userId,
        string code,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<TwoFactorStatusResponse?> GetTwoFactorStatusAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<TwoFactorSetupResponse?> BeginTwoFactorSetupAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<TwoFactorEnableResponse?> EnableTwoFactorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken);

    Task<TwoFactorStatusUpdateResponse?> DisableTwoFactorAsync(
        Guid userId,
        string currentPassword,
        CancellationToken cancellationToken);

    Task<TwoFactorRecoveryCodesResponse?> ResetTwoFactorRecoveryCodesAsync(
        Guid userId,
        string currentPassword,
        CancellationToken cancellationToken);

    Task<TwoFactorSetupResponse?> ResetAuthenticatorAsync(
        Guid userId,
        string currentPassword,
        CancellationToken cancellationToken);

    Task<AuthSessionResult?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<bool> RevokeRefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken);

    Task<int> RevokeAllRefreshTokensForUserAsync(
        Guid userId,
        AdminAuditActor actor,
        string reason,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminSessionResponse>> GetSessionsAsync(
        Guid userId,
        string? currentRefreshToken,
        CancellationToken cancellationToken);

    Task<AdminSessionRevocationResult?> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        string? currentRefreshToken,
        AdminAuditActor actor,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<AdminOtherSessionsRevocationResult?> RevokeOtherSessionsAsync(
        Guid userId,
        string? currentRefreshToken,
        AdminAuditActor actor,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<CurrentUserResponse?> ChangeOwnPasswordAsync(
        Guid currentUserId,
        ChangeOwnPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);
}
