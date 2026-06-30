namespace BespokeStudio.Application.Contracts.AdminUsers;

public sealed record CreateAdminUserRequest(
    string Email,
    string Password);
