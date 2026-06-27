using System.Security.Claims;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Auth;

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

        auth.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .Produces<CurrentUserResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.Unauthorized();
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null ? TypedResults.Unauthorized() : TypedResults.Ok(result);
    }

    private static IResult GetCurrentUser(ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (!Guid.TryParse(idValue, out var id) || string.IsNullOrWhiteSpace(email))
        {
            return TypedResults.Unauthorized();
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();

        return TypedResults.Ok(new CurrentUserResponse(id, email, roles));
    }
}
