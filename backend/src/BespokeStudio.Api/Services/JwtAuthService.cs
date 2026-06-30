using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BespokeStudio.Api.Configuration;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Auth;
using BespokeStudio.Application.Validation;
using BespokeStudio.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BespokeStudio.Api.Services;

public sealed class JwtAuthService(
    UserManager<AdminUser> userManager,
    SignInManager<AdminUser> signInManager,
    IAdminAuditLogService auditLogService,
    IOptions<JwtSettings> jwtSettings) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AuthTokenResponse?> LoginAsync(
        LoginRequest request,
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
        var expiresAt = DateTimeOffset.UtcNow.AddHours(_jwtSettings.ExpirationHours);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? email)
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
            User: new CurrentUserResponse(user.Id, user.Email ?? email, roles));
    }

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
