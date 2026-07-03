using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BespokeStudio.Api.Configuration;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Auth;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;
using BespokeStudio.Infrastructure.Authentication;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BespokeStudio.Api.Services;

public sealed class JwtAuthService(
    UserManager<AdminUser> userManager,
    SignInManager<AdminUser> signInManager,
    BespokeStudioDbContext dbContext,
    IAdminAuditLogService auditLogService,
    IOptions<JwtSettings> jwtSettings,
    IOptions<RefreshTokenSettings> refreshTokenSettings) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly RefreshTokenSettings _refreshTokenSettings = refreshTokenSettings.Value;

    public async Task<AuthSessionResult?> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            await RecordAuthEventAsync(
                null,
                email,
                "auth.login_failed",
                "Login was rejected (invalid_credentials).",
                cancellationToken);
            return null;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            await RecordAuthEventAsync(
                null,
                email,
                "auth.login_failed",
                "Login was rejected (invalid_credentials).",
                cancellationToken);
            return null;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            var category = signInResult.IsLockedOut ? "locked_out" : "invalid_credentials";
            await RecordAuthEventAsync(
                user,
                email,
                "auth.login_failed",
                $"Login was rejected ({category}).",
                cancellationToken);
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        if (!roles.Contains(AdminAccess.RoleName, StringComparer.Ordinal))
        {
            await RecordAuthEventAsync(
                user,
                email,
                "auth.login_failed",
                "Login was rejected (not_admin).",
                cancellationToken);
            return null;
        }

        var session = CreateRefreshToken(user.Id, Guid.NewGuid(), ipAddress, userAgent);
        dbContext.AdminRefreshTokens.Add(session.Entity);
        auditLogService.AddPending(CreateAuthEvent(
            user,
            email,
            "auth.login_succeeded",
            "Admin login succeeded."));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult(
            CreateAccessToken(user, roles, email),
            session.RawToken,
            session.Entity.ExpiresAtUtc);
    }

    public async Task<AuthSessionResult?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await RecordAuthEventAsync(
                null,
                null,
                "auth.refresh_failed",
                "Refresh was rejected (missing).",
                cancellationToken);
            return null;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.AdminRefreshTokens
            .FromSqlInterpolated($"SELECT * FROM \"AdminRefreshTokens\" WHERE \"TokenHash\" = {tokenHash} FOR UPDATE")
            .Include(token => token.User)
            .SingleOrDefaultAsync(cancellationToken);
        if (current is null)
        {
            auditLogService.AddPending(CreateAuthEvent(
                null,
                null,
                "auth.refresh_failed",
                "Refresh was rejected (invalid)."));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (current.RevokedAtUtc.HasValue)
        {
            await RevokeTokenFamilyAsync(current.TokenFamilyId, now, ipAddress, "refresh_reuse", cancellationToken);
            auditLogService.AddPending(CreateAuthEvent(
                current.User,
                current.User.Email,
                "auth.refresh_reuse_detected",
                "A revoked refresh token was reused; its token family was revoked."));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var failureCategory = current.ExpiresAtUtc <= now
            ? "expired"
            : await userManager.IsLockedOutAsync(current.User)
                ? "user_disabled"
                : !await userManager.IsInRoleAsync(current.User, AdminAccess.RoleName)
                    ? "not_admin"
                    : null;
        if (failureCategory is not null)
        {
            current.RevokedAtUtc = now;
            current.RevokedByIp = Trim(ipAddress, 64);
            current.RevocationReason = failureCategory;
            auditLogService.AddPending(CreateAuthEvent(
                current.User,
                current.User.Email,
                "auth.refresh_failed",
                $"Refresh was rejected ({failureCategory})."));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var replacement = CreateRefreshToken(current.UserId, current.TokenFamilyId, ipAddress, userAgent);
        current.LastUsedAtUtc = now;
        current.RevokedAtUtc = now;
        current.RevokedByIp = Trim(ipAddress, 64);
        current.RevocationReason = "rotated";
        current.ReplacedByTokenId = replacement.Entity.Id;
        dbContext.AdminRefreshTokens.Add(replacement.Entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var roles = (await userManager.GetRolesAsync(current.User)).ToArray();
        return new AuthSessionResult(
            CreateAccessToken(current.User, roles, current.User.Email ?? string.Empty),
            replacement.RawToken,
            replacement.Entity.ExpiresAtUtc);
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var token = await dbContext.AdminRefreshTokens
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
        if (token is null || token.RevokedAtUtc.HasValue)
        {
            return false;
        }

        token.RevokedAtUtc = DateTimeOffset.UtcNow;
        token.RevokedByIp = Trim(ipAddress, 64);
        token.RevocationReason = Trim(reason, 200);
        auditLogService.AddPending(CreateAuthEvent(
            token.User,
            token.User.Email,
            "auth.logout",
            "Admin session was logged out."));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllRefreshTokensForUserAsync(
        Guid userId,
        AdminAuditActor actor,
        string reason,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeTokens = await dbContext.AdminRefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.RevokedByIp = Trim(ipAddress, 64);
            token.RevocationReason = Trim(reason, 200);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        auditLogService.AddPending(new AdminAuditLogWriteRequest(
            actor.UserId,
            actor.Email,
            "auth.sessions_revoked",
            "AdminUser",
            userId.ToString(),
            user?.Email ?? user?.UserName,
            $"All active refresh sessions were revoked ({reason}); count: {activeTokens.Count}."));
        await dbContext.SaveChangesAsync(cancellationToken);
        return activeTokens.Count;
    }

    private AuthTokenResponse CreateAccessToken(AdminUser user, string[] roles, string fallbackEmail)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? fallbackEmail),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? fallbackEmail),
            new(AdminAccess.SecurityStampClaimType, user.SecurityStamp ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthTokenResponse(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
            TokenType: "Bearer",
            ExpiresAt: expiresAt,
            User: new CurrentUserResponse(user.Id, user.Email ?? fallbackEmail, roles));
    }

    private (AdminRefreshToken Entity, string RawToken) CreateRefreshToken(
        Guid userId,
        Guid familyId,
        string? ipAddress,
        string? userAgent)
    {
        var rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var now = DateTimeOffset.UtcNow;
        return (new AdminRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashRefreshToken(rawToken),
            TokenFamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(_refreshTokenSettings.LifetimeDays),
            CreatedByIp = Trim(ipAddress, 64),
            UserAgent = Trim(userAgent, 500)
        }, rawToken);
    }

    private async Task RevokeTokenFamilyAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.AdminRefreshTokens
            .Where(token => token.TokenFamilyId == familyId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = revokedAt;
            token.RevokedByIp = Trim(ipAddress, 64);
            token.RevocationReason = reason;
        }
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];

    public async Task<CurrentUserResponse?> ChangeOwnPasswordAsync(
        Guid currentUserId,
        ChangeOwnPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = AdminAccountValidator.Validate(request);
        if (errors.Count > 0)
        {
            throw new AdminAccountException(errors);
        }

        var user = await userManager.FindByIdAsync(currentUserId.ToString());
        if (user is null)
        {
            return null;
        }

        var changeResult = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);
        if (!changeResult.Succeeded)
        {
            ThrowPasswordChangeFailure(changeResult);
        }

        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            ThrowPasswordChangeFailure(stampResult);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var actor = new AdminAuditActor(user.Id, user.Email ?? user.UserName ?? "unknown-admin");
        await RevokeAllRefreshTokensForUserAsync(
            user.Id,
            actor,
            "password_changed",
            ipAddress,
            cancellationToken);
        await RecordPasswordChangedAsync(user, cancellationToken);

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return new CurrentUserResponse(user.Id, user.Email ?? user.UserName ?? string.Empty, roles);
    }

    private async Task RecordPasswordChangedAsync(AdminUser user, CancellationToken cancellationToken)
    {
        await auditLogService.RecordAsync(
            new AdminAuditLogWriteRequest(
                user.Id,
                user.Email ?? user.UserName ?? "unknown-admin",
                "account.password_changed",
                "AdminUser",
                user.Id.ToString(),
                user.Email ?? user.UserName,
                "Own admin password was changed."),
            cancellationToken);
    }

    private Task RecordAuthEventAsync(
        AdminUser? user,
        string? attemptedEmail,
        string action,
        string summary,
        CancellationToken cancellationToken) =>
        auditLogService.RecordAsync(
            CreateAuthEvent(user, attemptedEmail, action, summary),
            cancellationToken);

    private static AdminAuditLogWriteRequest CreateAuthEvent(
        AdminUser? user,
        string? attemptedEmail,
        string action,
        string summary)
    {
        var email = user?.Email ?? user?.UserName ?? attemptedEmail;
        return new AdminAuditLogWriteRequest(
            user?.Id,
            string.IsNullOrWhiteSpace(email) ? "unknown-admin" : email,
            action,
            "Authentication",
            user?.Id.ToString(),
            string.IsNullOrWhiteSpace(email) ? null : email,
            summary);
    }

    private static void ThrowPasswordChangeFailure(IdentityResult result)
    {
        var currentPasswordErrors = result.Errors
            .Where(error => string.Equals(error.Code, "PasswordMismatch", StringComparison.OrdinalIgnoreCase))
            .Select(error => "Current password is incorrect.")
            .ToArray();

        if (currentPasswordErrors.Length > 0)
        {
            throw new AdminAccountException(new Dictionary<string, string[]>
            {
                [nameof(ChangeOwnPasswordRequest.CurrentPassword)] = currentPasswordErrors
            });
        }

        throw new AdminAccountException(new Dictionary<string, string[]>
        {
            [nameof(ChangeOwnPasswordRequest.NewPassword)] = result.Errors.Select(error => error.Description).ToArray()
        });
    }
}
