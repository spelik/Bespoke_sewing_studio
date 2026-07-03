using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Auth;

namespace BespokeStudio.Application.Abstractions;

public interface IAuthService
{
    Task<AuthSessionResult?> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
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

    Task<CurrentUserResponse?> ChangeOwnPasswordAsync(
        Guid currentUserId,
        ChangeOwnPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);
}
