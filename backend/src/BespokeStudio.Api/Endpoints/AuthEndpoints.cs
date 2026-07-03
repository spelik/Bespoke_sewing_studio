using System.Security.Claims;
using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Auth;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;
using BespokeStudio.Api.Configuration;
using Microsoft.Extensions.Options;
using BespokeStudio.Application.Contracts.AdminAuditLog;

namespace BespokeStudio.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        auth.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .Produces<AuthTokenResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

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

        SetRefreshCookie(httpContext, refreshTokenSettings.Value, result);
        return TypedResults.Ok(result.Token);
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

    private static string? GetIpAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    private static string? GetUserAgent(HttpContext context) =>
        context.Request.Headers.UserAgent.ToString();

    private static Dictionary<string, string[]> ToJsonPropertyNames(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
