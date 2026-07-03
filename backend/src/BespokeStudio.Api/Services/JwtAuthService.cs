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

        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        if (!roles.Contains(AdminAccess.RoleName, StringComparer.Ordinal))
        {
            return null;
        }

        var session = CreateRefreshToken(user.Id, Guid.NewGuid(), ipAddress, userAgent);
        dbContext.AdminRefreshTokens.Add(session.Entity);
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
        var tokenHash = HashRefreshToken(refreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.AdminRefreshTokens
            .FromSqlInterpolated($"SELECT * FROM \"AdminRefreshTokens\" WHERE \"TokenHash\" = {tokenHash} FOR UPDATE")
            .Include(token => token.User)
            .SingleOrDefaultAsync(cancellationToken);
        if (current is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (current.RevokedAtUtc.HasValue)
        {
            await RevokeTokenFamilyAsync(current.TokenFamilyId, now, ipAddress, "Refresh token reuse detected.", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        if (current.ExpiresAtUtc <= now || await userManager.IsLockedOutAsync(current.User) ||
            !await userManager.IsInRoleAsync(current.User, AdminAccess.RoleName))
        {
            current.RevokedAtUtc = now;
            current.RevokedByIp = Trim(ipAddress, 64);
            current.RevocationReason = "Session is no longer valid.";
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var replacement = CreateRefreshToken(current.UserId, current.TokenFamilyId, ipAddress, userAgent);
        current.LastUsedAtUtc = now;
        current.RevokedAtUtc = now;
        current.RevokedByIp = Trim(ipAddress, 64);
        current.RevocationReason = "Rotated.";
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

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var token = await dbContext.AdminRefreshTokens
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
        if (token is null || token.RevokedAtUtc.HasValue)
        {
            return;
        }

        token.RevokedAtUtc = DateTimeOffset.UtcNow;
        token.RevokedByIp = Trim(ipAddress, 64);
        token.RevocationReason = Trim(reason, 200);
        await dbContext.SaveChangesAsync(cancellationToken);
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
            new(ClaimTypes.Email, user.Email ?? fallbackEmail)
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
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];

    public async Task<CurrentUserResponse?> ChangeOwnPasswordAsync(
        Guid currentUserId,
        ChangeOwnPasswordRequest request,
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

        await userManager.ResetAccessFailedCountAsync(user);
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
