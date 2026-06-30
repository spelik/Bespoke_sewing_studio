using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminUsers;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;
using BespokeStudio.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed class AdminUserManagementService(
    UserManager<AdminUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IAdminUserManagementService
{
    public async Task<IReadOnlyList<AdminUserResponse>> GetAllAsync(
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var users = await userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var activeAdminCount = await GetActiveAdminCountAsync(cancellationToken);
        var responses = new List<AdminUserResponse>(users.Count);

        foreach (var user in users)
        {
            responses.Add(await MapAsync(user, currentUserId, activeAdminCount));
        }

        return responses;
    }

    public async Task<AdminUserResponse> CreateAsync(
        Guid currentUserId,
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = AdminUserManagementValidator.Validate(request);
        if (errors.Count > 0)
        {
            throw new AdminUserManagementException(errors);
        }

        await EnsureAdminRoleAsync();

        var email = request.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw AdminUserManagementException.For(
                nameof(CreateAdminUserRequest.Email),
                "An admin user with this email already exists.");
        }

        var user = new AdminUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        ThrowIfFailed(createResult, nameof(CreateAdminUserRequest.Password));

        var roleResult = await userManager.AddToRoleAsync(user, AdminAccess.RoleName);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            ThrowIfFailed(roleResult, nameof(CreateAdminUserRequest.Email));
        }

        var activeAdminCount = await GetActiveAdminCountAsync(cancellationToken);
        return await MapAsync(user, currentUserId, activeAdminCount);
    }

    public async Task<AdminUserResponse?> SetDisabledAsync(
        Guid currentUserId,
        Guid userId,
        UpdateAdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (request.IsDisabled)
        {
            if (user.Id == currentUserId)
            {
                throw AdminUserManagementException.For("user", "You cannot disable your own admin account.");
            }

            if (await IsLastActiveAdminAsync(user, cancellationToken))
            {
                throw AdminUserManagementException.For("user", "You cannot disable the last active admin account.");
            }

            var lockoutResult = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            ThrowIfFailed(lockoutResult, "user");
        }
        else
        {
            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                var updateResult = await userManager.UpdateAsync(user);
                ThrowIfFailed(updateResult, "user");
            }

            var lockoutResult = await userManager.SetLockoutEndDateAsync(user, null);
            ThrowIfFailed(lockoutResult, "user");
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var activeAdminCount = await GetActiveAdminCountAsync(cancellationToken);
        return await MapAsync(user, currentUserId, activeAdminCount);
    }

    public async Task<AdminUserResponse?> ResetPasswordAsync(
        Guid currentUserId,
        Guid userId,
        ResetAdminUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = AdminUserManagementValidator.Validate(request);
        if (errors.Count > 0)
        {
            throw new AdminUserManagementException(errors);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, request.Password);
        ThrowIfFailed(resetResult, nameof(ResetAdminUserPasswordRequest.Password));
        await userManager.ResetAccessFailedCountAsync(user);

        var activeAdminCount = await GetActiveAdminCountAsync(cancellationToken);
        return await MapAsync(user, currentUserId, activeAdminCount);
    }

    public async Task<bool?> DeleteAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (user.Id == currentUserId)
        {
            throw AdminUserManagementException.For("user", "You cannot delete your own admin account.");
        }

        if (await IsLastActiveAdminAsync(user, cancellationToken))
        {
            throw AdminUserManagementException.For("user", "You cannot delete the last active admin account.");
        }

        var deleteResult = await userManager.DeleteAsync(user);
        ThrowIfFailed(deleteResult, "user");
        return true;
    }

    private async Task EnsureAdminRoleAsync()
    {
        if (await roleManager.RoleExistsAsync(AdminAccess.RoleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(AdminAccess.RoleName));
        ThrowIfFailed(result, nameof(CreateAdminUserRequest.Email));
    }

    private async Task<bool> IsLastActiveAdminAsync(AdminUser user, CancellationToken cancellationToken)
    {
        if (IsDisabled(user))
        {
            return false;
        }

        var activeAdminCount = await GetActiveAdminCountAsync(cancellationToken);
        return activeAdminCount <= 1;
    }

    private async Task<int> GetActiveAdminCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var adminUsers = await userManager.GetUsersInRoleAsync(AdminAccess.RoleName);
        return adminUsers.Count(user => !IsDisabled(user));
    }

    private async Task<AdminUserResponse> MapAsync(
        AdminUser user,
        Guid currentUserId,
        int activeAdminCount)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var isCurrentUser = user.Id == currentUserId;
        var isDisabled = IsDisabled(user);
        var canDisable = !isCurrentUser && (isDisabled || activeAdminCount > 1);
        var canDelete = !isCurrentUser && (isDisabled || activeAdminCount > 1);

        return new AdminUserResponse(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            roles,
            isCurrentUser,
            isDisabled,
            canDisable,
            canDelete,
            user.LockoutEnd);
    }

    private static bool IsDisabled(AdminUser user) =>
        user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

    private static void ThrowIfFailed(IdentityResult result, string propertyName)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new AdminUserManagementException(new Dictionary<string, string[]>
        {
            [propertyName] = result.Errors.Select(error => error.Description).ToArray()
        });
    }
}
