using BespokeStudio.Application.Contracts.Auth;

namespace BespokeStudio.Application.Abstractions;

public interface IAuthService
{
    Task<AuthTokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
