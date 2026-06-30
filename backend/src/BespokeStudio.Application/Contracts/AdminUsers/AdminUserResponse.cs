namespace BespokeStudio.Application.Contracts.AdminUsers;

public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsCurrentUser,
    bool IsDisabled,
    bool CanDisable,
    bool CanDelete,
    DateTimeOffset? LockoutEnd);
