using BespokeStudio.Application.Contracts.AdminUsers;

namespace BespokeStudio.Application.Abstractions;

public interface IAdminUserManagementService
{
    Task<IReadOnlyList<AdminUserResponse>> GetAllAsync(Guid currentUserId, CancellationToken cancellationToken);

    Task<AdminUserResponse> CreateAsync(
        Guid currentUserId,
        CreateAdminUserRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserResponse?> SetDisabledAsync(
        Guid currentUserId,
        Guid userId,
        UpdateAdminUserStatusRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserResponse?> ResetPasswordAsync(
        Guid currentUserId,
        Guid userId,
        ResetAdminUserPasswordRequest request,
        CancellationToken cancellationToken);

    Task<bool?> DeleteAsync(Guid currentUserId, Guid userId, CancellationToken cancellationToken);
}
