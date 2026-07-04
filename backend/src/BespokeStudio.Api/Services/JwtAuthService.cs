using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BespokeStudio.Api.Services;

public sealed class JwtAuthService(
    UserManager<AdminUser> userManager,
    SignInManager<AdminUser> signInManager,
    BespokeStudioDbContext dbContext,
    IAdminAuditLogService auditLogService,
    IOptions<JwtSettings> jwtSettings,
    IOptions<RefreshTokenSettings> refreshTokenSettings,
    ILogger<JwtAuthService> logger) : IAuthService
{
    private const string TwoFactorIssuer = "Bespoke Sewing Studio";
    private const int RecoveryCodeCount = 10;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly RefreshTokenSettings _refreshTokenSettings = refreshTokenSettings.Value;

    public async Task<AuthLoginResult?> LoginAsync(
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

        if (user.TwoFactorEnabled)
        {
            await RecordAuthEventAsync(
                user,
                email,
                "auth.2fa_challenge_required",
                "Password verification succeeded; a two-factor challenge is required.",
                cancellationToken);
            return new AuthLoginResult(null, user.Id);
        }

        return new AuthLoginResult(
            await CreateSessionAsync(
                user,
                roles,
                email,
                ipAddress,
                userAgent,
                "auth.login_succeeded",
                "Admin login succeeded.",
                cancellationToken),
            null);
    }

    public async Task<AuthSessionResult?> VerifyTwoFactorAsync(
        Guid userId,
        string code,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null ||
            !user.TwoFactorEnabled ||
            await userManager.IsLockedOutAsync(user) ||
            !await userManager.IsInRoleAsync(user, AdminAccess.RoleName))
        {
            await RecordAuthEventAsync(
                user,
                user?.Email,
                "auth.2fa_challenge_failed",
                "Two-factor verification was rejected.",
                cancellationToken);
            return null;
        }

        var submittedCode = code?.Trim() ?? string.Empty;
        var authenticatorCode = NormalizeAuthenticatorCode(submittedCode);
        var isValid = authenticatorCode.Length > 0 &&
            await userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                authenticatorCode);
        if (!isValid && submittedCode.Length > 0)
        {
            var recoveryResult = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, submittedCode);
            isValid = recoveryResult.Succeeded;
        }

        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            await RecordAuthEventAsync(
                user,
                user.Email,
                "auth.2fa_challenge_failed",
                "Two-factor verification was rejected.",
                cancellationToken);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return await CreateSessionAsync(
            user,
            roles,
            user.Email ?? user.UserName ?? string.Empty,
            ipAddress,
            userAgent,
            "auth.2fa_challenge_succeeded",
            "Two-factor verification succeeded and an admin session was created.",
            cancellationToken);
    }

    public async Task<TwoFactorStatusResponse?> GetTwoFactorStatusAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await CreateTwoFactorStatusAsync(user);
    }

    public async Task<TwoFactorSetupResponse?> BeginTwoFactorSetupAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (user.TwoFactorEnabled)
        {
            throw CreateTwoFactorError("Two-factor authentication is already enabled.");
        }

        var response = await GetOrCreateAuthenticatorSetupAsync(user);
        await RecordAuthEventAsync(
            user,
            user.Email,
            "auth.2fa_setup_started",
            "Authenticator setup was started.",
            cancellationToken);
        return response;
    }

    public async Task<TwoFactorEnableResponse?> EnableTwoFactorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (user.TwoFactorEnabled)
        {
            throw CreateTwoFactorError("Two-factor authentication is already enabled.");
        }

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        var normalizedCode = NormalizeAuthenticatorCode(code);
        if (string.IsNullOrWhiteSpace(key) ||
            string.IsNullOrWhiteSpace(normalizedCode) ||
            !await userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultAuthenticatorProvider,
                normalizedCode))
        {
            await RecordAuthEventAsync(
                user,
                user.Email,
                "auth.2fa_enable_failed",
                "Two-factor enablement was rejected because the verification code was invalid.",
                cancellationToken);
            throw CreateTwoFactorError("The authenticator code is invalid.", nameof(TwoFactorCodeRequest.Code));
        }

        var enableResult = await userManager.SetTwoFactorEnabledAsync(user, true);
        ThrowTwoFactorFailure(enableResult);

        string[] recoveryCodes;
        try
        {
            recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount)
                ?? []).ToArray();
        }
        catch
        {
            await userManager.SetTwoFactorEnabledAsync(user, false);
            throw;
        }

        if (recoveryCodes.Length != RecoveryCodeCount)
        {
            await userManager.SetTwoFactorEnabledAsync(user, false);
            await RecordAuthEventAsync(
                user,
                user.Email,
                "auth.2fa_enable_failed",
                "Two-factor enablement failed because recovery codes could not be generated.",
                cancellationToken);
            throw CreateTwoFactorError("Recovery codes could not be generated. Two-factor authentication was not enabled.");
        }
        await RecordAuthEventAsync(
            user,
            user.Email,
            "auth.2fa_enabled",
            "Two-factor authentication was enabled.",
            cancellationToken);

        return new TwoFactorEnableResponse(
            await CreateTwoFactorStatusAsync(user),
            recoveryCodes,
            await CreateCurrentAccessTokenAsync(user));
    }

    public async Task<TwoFactorStatusUpdateResponse?> DisableTwoFactorAsync(
        Guid userId,
        string currentPassword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        await RequireCurrentPasswordAsync(user, currentPassword);
        var result = await userManager.SetTwoFactorEnabledAsync(user, false);
        ThrowTwoFactorFailure(result);
        await RecordAuthEventAsync(
            user,
            user.Email,
            "auth.2fa_disabled",
            "Two-factor authentication was disabled.",
            cancellationToken);
        return new TwoFactorStatusUpdateResponse(
            await CreateTwoFactorStatusAsync(user),
            await CreateCurrentAccessTokenAsync(user));
    }

    public async Task<TwoFactorRecoveryCodesResponse?> ResetTwoFactorRecoveryCodesAsync(
        Guid userId,
        string currentPassword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (!user.TwoFactorEnabled)
        {
            throw CreateTwoFactorError("Enable two-factor authentication before resetting recovery codes.");
        }

        await RequireCurrentPasswordAsync(user, currentPassword);
        var recoveryCodes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount)
            ?? []).ToArray();
        await RecordAuthEventAsync(
            user,
            user.Email,
            "auth.2fa_recovery_codes_regenerated",
            "Two-factor recovery codes were regenerated.",
            cancellationToken);
        return new TwoFactorRecoveryCodesResponse(
            recoveryCodes,
            recoveryCodes.Length,
            await CreateCurrentAccessTokenAsync(user));
    }

    public async Task<TwoFactorSetupResponse?> ResetAuthenticatorAsync(
        Guid userId,
        string currentPassword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        await RequireCurrentPasswordAsync(user, currentPassword);
        ThrowTwoFactorFailure(await userManager.SetTwoFactorEnabledAsync(user, false));
        ThrowTwoFactorFailure(await userManager.ResetAuthenticatorKeyAsync(user));
        var response = await GetOrCreateAuthenticatorSetupAsync(user);
        await RecordAuthEventAsync(
            user,
            user.Email,
            "auth.2fa_authenticator_reset",
            "The authenticator key was reset and two-factor authentication was disabled pending setup.",
            cancellationToken);
        return response;
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

    public async Task<IReadOnlyList<AdminSessionResponse>> GetSessionsAsync(
        Guid userId,
        string? currentRefreshToken,
        CancellationToken cancellationToken)
    {
        var tokens = await dbContext.AdminRefreshTokens
            .AsNoTracking()
            .Where(token => token.UserId == userId)
            .OrderByDescending(token => token.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var currentTokenId = FindCurrentTokenId(tokens, currentRefreshToken);
        var now = DateTimeOffset.UtcNow;

        return tokens
            .GroupBy(token => token.TokenFamilyId)
            .Select(family =>
            {
                var latest = family.OrderByDescending(token => token.CreatedAtUtc).First();
                var isCurrent = currentTokenId.HasValue && family.Any(token => token.Id == currentTokenId.Value);
                var isRevoked = latest.RevokedAtUtc.HasValue;
                var isExpired = latest.ExpiresAtUtc <= now;
                var status = isCurrent && !isRevoked && !isExpired
                    ? "Current"
                    : isRevoked
                        ? "Revoked"
                        : isExpired
                            ? "Expired"
                            : "Active";

                return new AdminSessionResponse(
                    latest.Id,
                    family.Min(token => token.CreatedAtUtc),
                    latest.ExpiresAtUtc,
                    family.Max(token => token.LastUsedAtUtc),
                    latest.RevokedAtUtc,
                    isCurrent,
                    isRevoked,
                    SanitizeForDisplay(latest.UserAgent, 180),
                    MaskIpAddress(latest.CreatedByIp),
                    SanitizeForDisplay(latest.RevocationReason, 100),
                    status);
            })
            .OrderByDescending(session => session.IsCurrent)
            .ThenBy(session => session.Status is "Active" ? 0 : 1)
            .ThenByDescending(session => session.CreatedAtUtc)
            .ToArray();
    }

    public async Task<AdminSessionRevocationResult?> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        string? currentRefreshToken,
        AdminAuditActor actor,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var token = await dbContext.AdminRefreshTokens
            .SingleOrDefaultAsync(
                candidate => candidate.Id == sessionId && candidate.UserId == userId,
                cancellationToken);
        if (token is null)
        {
            return null;
        }

        var currentTokenId = await FindCurrentTokenIdAsync(userId, currentRefreshToken, cancellationToken);
        var isCurrent = currentTokenId == token.Id;
        var now = DateTimeOffset.UtcNow;
        var revoked = !token.RevokedAtUtc.HasValue && token.ExpiresAtUtc > now;
        if (revoked)
        {
            token.RevokedAtUtc = now;
            token.RevokedByIp = Trim(ipAddress, 64);
            token.RevocationReason = "session_revoked";
            auditLogService.AddPending(new AdminAuditLogWriteRequest(
                actor.UserId,
                actor.Email,
                "auth.session_revoked",
                "AdminSession",
                token.Id.ToString(),
                isCurrent ? "Current session" : "Admin session",
                $"An admin session was revoked; current session: {isCurrent}."));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AdminSessionRevocationResult(token.Id, revoked, isCurrent);
    }

    public async Task<AdminOtherSessionsRevocationResult?> RevokeOtherSessionsAsync(
        Guid userId,
        string? currentRefreshToken,
        AdminAuditActor actor,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var currentTokenId = await FindCurrentTokenIdAsync(userId, currentRefreshToken, cancellationToken);
        if (!currentTokenId.HasValue)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var otherTokens = await dbContext.AdminRefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.Id != currentTokenId.Value &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
        foreach (var token in otherTokens)
        {
            token.RevokedAtUtc = now;
            token.RevokedByIp = Trim(ipAddress, 64);
            token.RevocationReason = "other_sessions_revoked";
        }

        auditLogService.AddPending(new AdminAuditLogWriteRequest(
            actor.UserId,
            actor.Email,
            "auth.other_sessions_revoked",
            "AdminUser",
            userId.ToString(),
            actor.Email,
            $"Other active admin sessions were revoked; count: {otherTokens.Count}."));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdminOtherSessionsRevocationResult(otherTokens.Count);
    }

    private async Task<AuthSessionResult> CreateSessionAsync(
        AdminUser user,
        string[] roles,
        string fallbackEmail,
        string? ipAddress,
        string? userAgent,
        string auditAction,
        string auditSummary,
        CancellationToken cancellationToken)
    {
        var session = CreateRefreshToken(user.Id, Guid.NewGuid(), ipAddress, userAgent);
        dbContext.AdminRefreshTokens.Add(session.Entity);
        auditLogService.AddPending(CreateAuthEvent(
            user,
            fallbackEmail,
            auditAction,
            auditSummary));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult(
            CreateAccessToken(user, roles, fallbackEmail),
            session.RawToken,
            session.Entity.ExpiresAtUtc);
    }

    private async Task<TwoFactorStatusResponse> CreateTwoFactorStatusAsync(AdminUser user)
    {
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        var recoveryCodesRemaining = user.TwoFactorEnabled
            ? await userManager.CountRecoveryCodesAsync(user)
            : 0;
        return new TwoFactorStatusResponse(
            user.TwoFactorEnabled,
            !string.IsNullOrWhiteSpace(key),
            recoveryCodesRemaining);
    }

    private async Task<AuthTokenResponse> CreateCurrentAccessTokenAsync(AdminUser user)
    {
        user.SecurityStamp = await userManager.GetSecurityStampAsync(user);
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return CreateAccessToken(
            user,
            roles,
            user.Email ?? user.UserName ?? string.Empty);
    }

    private async Task<TwoFactorSetupResponse> GetOrCreateAuthenticatorSetupAsync(AdminUser user)
    {
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            ThrowTwoFactorFailure(await userManager.ResetAuthenticatorKeyAsync(user));
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw CreateTwoFactorError("Authenticator setup could not be created.");
        }

        var accountName = user.Email ?? user.UserName ?? user.Id.ToString();
        var authenticatorUri =
            $"otpauth://totp/{Uri.EscapeDataString(TwoFactorIssuer)}:{Uri.EscapeDataString(accountName)}" +
            $"?secret={Uri.EscapeDataString(key)}&issuer={Uri.EscapeDataString(TwoFactorIssuer)}&digits=6";
        return new TwoFactorSetupResponse(
            FormatAuthenticatorKey(key),
            authenticatorUri,
            TwoFactorIssuer,
            accountName,
            await CreateCurrentAccessTokenAsync(user));
    }

    private async Task RequireCurrentPasswordAsync(AdminUser user, string currentPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) ||
            !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            throw CreateTwoFactorError(
                "Current password is incorrect.",
                nameof(TwoFactorPasswordRequest.CurrentPassword));
        }
    }

    private static string NormalizeAuthenticatorCode(string? code) =>
        new((code ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .ToArray());

    private static string FormatAuthenticatorKey(string key) =>
        string.Join(' ', Enumerable.Range(0, (key.Length + 3) / 4)
            .Select(index => key.Substring(index * 4, Math.Min(4, key.Length - index * 4))))
        .ToUpperInvariant();

    private static AdminAccountException CreateTwoFactorError(
        string message,
        string field = "TwoFactor") =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static void ThrowTwoFactorFailure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw CreateTwoFactorError(
                result.Errors.Select(error => error.Description).FirstOrDefault()
                ?? "The two-factor request could not be completed.");
        }
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

    private async Task<Guid?> FindCurrentTokenIdAsync(
        Guid userId,
        string? currentRefreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            return null;
        }

        var tokenHash = HashRefreshToken(currentRefreshToken);
        return await dbContext.AdminRefreshTokens
            .Where(token => token.UserId == userId && token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static Guid? FindCurrentTokenId(
        IReadOnlyCollection<AdminRefreshToken> tokens,
        string? currentRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(currentRefreshToken))
        {
            return null;
        }

        var tokenHash = HashRefreshToken(currentRefreshToken);
        return tokens.FirstOrDefault(token => token.TokenHash == tokenHash)?.Id;
    }

    private static string? SanitizeForDisplay(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = new string(value.Trim().Where(character => !char.IsControl(character)).ToArray());
        return sanitized[..Math.Min(sanitized.Length, maxLength)];
    }

    private static string? MaskIpAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value, out var address))
        {
            return null;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.xxx";
        }

        var segments = address.ToString().Split(':', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? null : $"{string.Join(':', segments.Take(4))}:*";
    }

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

    private async Task RecordAuthEventAsync(
        AdminUser? user,
        string? attemptedEmail,
        string action,
        string summary,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditLogService.RecordAsync(
                CreateAuthEvent(user, attemptedEmail, action, summary),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A rejected authentication attempt must always surface a generic 401, never a 500.
            // If persisting the audit trail fails, log it safely (no credentials/tokens/cookies) and continue.
            logger.LogWarning(
                exception,
                "Failed to persist authentication audit event {AuditAction}; returning generic rejection.",
                action);
        }
    }

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
