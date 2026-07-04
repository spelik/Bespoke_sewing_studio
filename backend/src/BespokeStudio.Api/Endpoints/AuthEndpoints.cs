using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Auth;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;
using BespokeStudio.Api.Configuration;
using Microsoft.Extensions.Options;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using Microsoft.AspNetCore.DataProtection;

namespace BespokeStudio.Api.Endpoints;

public static class AuthEndpoints
{
    private const string TwoFactorChallengeCookieName = "BespokeStudio.Admin2FA";
    private const string TwoFactorChallengeProtectorPurpose = "BespokeStudio.Admin2FA.Challenge.v1";
    private static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        auth.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.AuthLogin)
            .WithName("Login")
            .Produces<AuthTokenResponse>()
            .Produces<TwoFactorRequiredResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/2fa/verify", VerifyTwoFactorAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.AuthTwoFactor)
            .WithName("VerifyAdminTwoFactorChallenge")
            .Produces<AuthTokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .WithName("RefreshAdminSession")
            .Produces<AuthTokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout", LogoutAsync)
            .AllowAnonymous()
            .WithName("LogoutAdminSession")
            .Produces(StatusCodes.Status204NoContent);

        auth.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .Produces<CurrentUserResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        auth.MapPost("/me/password", ChangeOwnPasswordAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("ChangeOwnAdminPassword")
            .Produces<CurrentUserResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapGet("/2fa/status", GetTwoFactorStatusAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("GetAdminTwoFactorStatus")
            .Produces<TwoFactorStatusResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapPost("/2fa/setup", BeginTwoFactorSetupAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("BeginAdminTwoFactorSetup")
            .Produces<TwoFactorSetupResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapPost("/2fa/enable", EnableTwoFactorAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("EnableAdminTwoFactor")
            .Produces<TwoFactorEnableResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapPost("/2fa/disable", DisableTwoFactorAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("DisableAdminTwoFactor")
            .Produces<TwoFactorStatusUpdateResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapPost("/2fa/recovery-codes/reset", ResetTwoFactorRecoveryCodesAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("ResetAdminTwoFactorRecoveryCodes")
            .Produces<TwoFactorRecoveryCodesResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapPost("/2fa/authenticator/reset", ResetAuthenticatorAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("ResetAdminAuthenticator")
            .Produces<TwoFactorSetupResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapGet("/sessions", GetSessionsAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("GetAdminSessions")
            .Produces<IReadOnlyList<AdminSessionResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapPost("/sessions/{id:guid}/revoke", RevokeSessionAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("RevokeAdminSession")
            .Produces<AdminSessionRevocationResult>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        auth.MapPost("/sessions/revoke-others", RevokeOtherSessionsAsync)
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithName("RevokeOtherAdminSessions")
            .Produces<AdminOtherSessionsRevocationResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        IAuthService authService,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            request,
            GetIpAddress(httpContext),
            GetUserAgent(httpContext),
            cancellationToken);
        if (result is null)
        {
            return TypedResults.Unauthorized();
        }

        if (result.RequiresTwoFactor && result.TwoFactorUserId.HasValue)
        {
            SetTwoFactorChallengeCookie(httpContext, dataProtectionProvider, result.TwoFactorUserId.Value);
            return TypedResults.Ok(new TwoFactorRequiredResponse());
        }

        if (result.Session is null)
        {
            return TypedResults.Unauthorized();
        }

        SetRefreshCookie(httpContext, refreshTokenSettings.Value, result.Session);
        return TypedResults.Ok(result.Session.Token);
    }

    private static async Task<IResult> VerifyTwoFactorAsync(
        TwoFactorCodeRequest request,
        HttpContext httpContext,
        IAuthService authService,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var userId = GetTwoFactorChallengeUserId(httpContext, dataProtectionProvider);
        if (!userId.HasValue)
        {
            DeleteTwoFactorChallengeCookie(httpContext);
            return TypedResults.Unauthorized();
        }

        var session = await authService.VerifyTwoFactorAsync(
            userId.Value,
            request.Code,
            GetIpAddress(httpContext),
            GetUserAgent(httpContext),
            cancellationToken);
        if (session is null)
        {
            return TypedResults.Unauthorized();
        }

        DeleteTwoFactorChallengeCookie(httpContext);
        SetRefreshCookie(httpContext, refreshTokenSettings.Value, session);
        return TypedResults.Ok(session.Token);
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext httpContext,
        IAuthService authService,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var settings = refreshTokenSettings.Value;
        httpContext.Request.Cookies.TryGetValue(settings.CookieName, out var refreshToken);

        var result = await authService.RefreshAsync(
            refreshToken ?? string.Empty,
            GetIpAddress(httpContext),
            GetUserAgent(httpContext),
            cancellationToken);
        if (result is null)
        {
            DeleteRefreshCookie(httpContext, settings);
            return TypedResults.Unauthorized();
        }

        SetRefreshCookie(httpContext, settings, result);
        return TypedResults.Ok(result.Token);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IAuthService authService,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var settings = refreshTokenSettings.Value;
        if (httpContext.Request.Cookies.TryGetValue(settings.CookieName, out var refreshToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
        {
            await authService.RevokeRefreshTokenAsync(
                refreshToken,
                GetIpAddress(httpContext),
                "logout",
                cancellationToken);
        }

        DeleteRefreshCookie(httpContext, settings);
        return TypedResults.NoContent();
    }

    private static IResult GetCurrentUser(ClaimsPrincipal principal)
    {
        var user = GetCurrentUserFromPrincipal(principal);
        return user is null ? TypedResults.Unauthorized() : TypedResults.Ok(user);
    }

    private static async Task<IResult> ChangeOwnPasswordAsync(
        ChangeOwnPasswordRequest request,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        IAuthService authService,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (currentUserId is null)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var user = await authService.ChangeOwnPasswordAsync(
                currentUserId.Value,
                request,
                GetIpAddress(httpContext),
                cancellationToken);

            if (user is null)
            {
                return TypedResults.Unauthorized();
            }

            DeleteRefreshCookie(httpContext, refreshTokenSettings.Value);
            return TypedResults.Ok(user);
        }
        catch (AdminAccountException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> GetTwoFactorStatusAsync(
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(principal);
        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var status = await authService.GetTwoFactorStatusAsync(userId.Value, cancellationToken);
        return status is null ? TypedResults.Unauthorized() : TypedResults.Ok(status);
    }

    private static async Task<IResult> BeginTwoFactorSetupAsync(
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(principal);
        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var setup = await authService.BeginTwoFactorSetupAsync(userId.Value, cancellationToken);
            return setup is null ? TypedResults.Unauthorized() : TypedResults.Ok(setup);
        }
        catch (AdminAccountException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> EnableTwoFactorAsync(
        TwoFactorCodeRequest request,
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(principal);
        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var result = await authService.EnableTwoFactorAsync(userId.Value, request.Code, cancellationToken);
            return result is null ? TypedResults.Unauthorized() : TypedResults.Ok(result);
        }
        catch (AdminAccountException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> DisableTwoFactorAsync(
        TwoFactorPasswordRequest request,
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(principal);
        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var status = await authService.DisableTwoFactorAsync(
                userId.Value,
                request.CurrentPassword,
                cancellationToken);
            return status is null ? TypedResults.Unauthorized() : TypedResults.Ok(status);
        }
        catch (AdminAccountException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> ResetTwoFactorRecoveryCodesAsync(
        TwoFactorPasswordRequest request,
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(principal);
        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var result = await authService.ResetTwoFactorRecoveryCodesAsync(
                userId.Value,
                request.CurrentPassword,
                cancellationToken);
            return result is null ? TypedResults.Unauthorized() : TypedResults.Ok(result);
        }
        catch (AdminAccountException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> ResetAuthenticatorAsync(
        TwoFactorPasswordRequest request,
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId(principal);
        if (!userId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var setup = await authService.ResetAuthenticatorAsync(
                userId.Value,
                request.CurrentPassword,
                cancellationToken);
            return setup is null ? TypedResults.Unauthorized() : TypedResults.Ok(setup);
        }
        catch (AdminAccountException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static CurrentUserResponse? GetCurrentUserFromPrincipal(ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (!Guid.TryParse(idValue, out var id) || string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();

        return new CurrentUserResponse(id, email, roles);
    }

    private static async Task<IResult> GetSessionsAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        IAuthService authService,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (!currentUserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        httpContext.Request.Cookies.TryGetValue(
            refreshTokenSettings.Value.CookieName,
            out var currentRefreshToken);
        var sessions = await authService.GetSessionsAsync(
            currentUserId.Value,
            currentRefreshToken,
            cancellationToken);
        return TypedResults.Ok(sessions);
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        IAuthService authService,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (!currentUserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        var settings = refreshTokenSettings.Value;
        httpContext.Request.Cookies.TryGetValue(settings.CookieName, out var currentRefreshToken);
        var result = await authService.RevokeSessionAsync(
            currentUserId.Value,
            id,
            currentRefreshToken,
            GetAuditActor(principal),
            GetIpAddress(httpContext),
            cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        if (result.IsCurrent)
        {
            DeleteRefreshCookie(httpContext, settings);
        }

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> RevokeOtherSessionsAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        IAuthService authService,
        IOptions<RefreshTokenSettings> refreshTokenSettings,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (!currentUserId.HasValue)
        {
            return TypedResults.Unauthorized();
        }

        httpContext.Request.Cookies.TryGetValue(
            refreshTokenSettings.Value.CookieName,
            out var currentRefreshToken);
        var result = await authService.RevokeOtherSessionsAsync(
            currentUserId.Value,
            currentRefreshToken,
            GetAuditActor(principal),
            GetIpAddress(httpContext),
            cancellationToken);
        return result is null
            ? TypedResults.Problem(
                title: "Current session is unavailable.",
                detail: "Sign in again before revoking other sessions.",
                statusCode: StatusCodes.Status400BadRequest)
            : TypedResults.Ok(result);
    }

    private static AdminAuditActor GetAuditActor(ClaimsPrincipal principal) =>
        new(
            GetCurrentUserId(principal),
            principal.FindFirstValue(ClaimTypes.Email) ?? "unknown-admin");

    private static Guid? GetCurrentUserId(ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : null;
    }

    private static void SetRefreshCookie(
        HttpContext context,
        RefreshTokenSettings settings,
        AuthSessionResult session)
    {
        context.Response.Cookies.Append(settings.CookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = session.RefreshTokenExpiresAt,
            IsEssential = true,
            Path = "/api/auth"
        });
    }

    private static void DeleteRefreshCookie(HttpContext context, RefreshTokenSettings settings) =>
        context.Response.Cookies.Delete(settings.CookieName, new CookieOptions
        {
            Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth"
        });

    private static void SetTwoFactorChallengeCookie(
        HttpContext context,
        IDataProtectionProvider dataProtectionProvider,
        Guid userId)
    {
        var protector = dataProtectionProvider
            .CreateProtector(TwoFactorChallengeProtectorPurpose)
            .ToTimeLimitedDataProtector();
        var protectedUserId = protector.Protect(userId.ToString(), TwoFactorChallengeLifetime);
        context.Response.Cookies.Append(
            TwoFactorChallengeCookieName,
            protectedUserId,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                MaxAge = TwoFactorChallengeLifetime,
                IsEssential = true,
                Path = "/api/auth"
            });
    }

    private static Guid? GetTwoFactorChallengeUserId(
        HttpContext context,
        IDataProtectionProvider dataProtectionProvider)
    {
        if (!context.Request.Cookies.TryGetValue(TwoFactorChallengeCookieName, out var protectedUserId) ||
            string.IsNullOrWhiteSpace(protectedUserId))
        {
            return null;
        }

        try
        {
            var protector = dataProtectionProvider
                .CreateProtector(TwoFactorChallengeProtectorPurpose)
                .ToTimeLimitedDataProtector();
            return Guid.TryParse(protector.Unprotect(protectedUserId), out var userId)
                ? userId
                : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static void DeleteTwoFactorChallengeCookie(HttpContext context) =>
        context.Response.Cookies.Delete(
            TwoFactorChallengeCookieName,
            new CookieOptions
            {
                Secure = !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Path = "/api/auth"
            });

    private static string? GetIpAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    private static string? GetUserAgent(HttpContext context) =>
        context.Request.Headers.UserAgent.ToString();

    private static Dictionary<string, string[]> ToJsonPropertyNames(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
