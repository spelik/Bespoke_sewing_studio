using System.Security.Claims;
using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminUsers;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class AdminUserEndpoints
{
    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/users")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Users");

        admin.MapGet(string.Empty, GetAllAsync)
            .WithName("GetAdminUsers")
            .Produces<IReadOnlyList<AdminUserResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPost(string.Empty, CreateAsync)
            .WithName("CreateAdminUser")
            .Produces<AdminUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPatch("/{id:guid}/status", SetDisabledAsync)
            .WithName("UpdateAdminUserStatus")
            .Produces<AdminUserResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPost("/{id:guid}/reset-password", ResetPasswordAsync)
            .WithName("ResetAdminUserPassword")
            .Produces<AdminUserResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteAdminUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetAllAsync(
        ClaimsPrincipal principal,
        IAdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (currentUserId is null)
        {
            return TypedResults.Unauthorized();
        }

        var users = await service.GetAllAsync(currentUserId.Value, cancellationToken);
        return TypedResults.Ok(users);
    }

    private static async Task<IResult> CreateAsync(
        CreateAdminUserRequest request,
        ClaimsPrincipal principal,
        IAdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (currentUserId is null)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var user = await service.CreateAsync(currentUserId.Value, request, cancellationToken);
            return TypedResults.Created($"/api/admin/users/{user.Id}", user);
        }
        catch (AdminUserManagementException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> SetDisabledAsync(
        Guid id,
        UpdateAdminUserStatusRequest request,
        ClaimsPrincipal principal,
        IAdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (currentUserId is null)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var user = await service.SetDisabledAsync(currentUserId.Value, id, request, cancellationToken);
            return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
        }
        catch (AdminUserManagementException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> ResetPasswordAsync(
        Guid id,
        ResetAdminUserPasswordRequest request,
        ClaimsPrincipal principal,
        IAdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (currentUserId is null)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var user = await service.ResetPasswordAsync(currentUserId.Value, id, request, cancellationToken);
            return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
        }
        catch (AdminUserManagementException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ClaimsPrincipal principal,
        IAdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(principal);
        if (currentUserId is null)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var deleted = await service.DeleteAsync(currentUserId.Value, id, cancellationToken);
            return deleted is null ? TypedResults.NotFound() : TypedResults.NoContent();
        }
        catch (AdminUserManagementException exception)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(exception.Errors));
        }
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : null;
    }

    private static Dictionary<string, string[]> ToJsonPropertyNames(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
