namespace BespokeStudio.Application.Contracts.Auth;

public sealed record CurrentUserResponse(Guid Id, string Email, IReadOnlyList<string> Roles);
