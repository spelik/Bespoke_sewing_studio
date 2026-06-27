using BespokeStudio.Application.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Authentication;

public static class AdminIdentitySeeder
{
    public static async Task SeedAdminIdentityAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var email = configuration["SeedAdmin:Email"]?.Trim();
        var password = configuration["SeedAdmin:Password"];

        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminIdentitySeeder");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "Admin seed skipped. Configure SeedAdmin:Email and SeedAdmin:Password to create a development administrator.");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync(AdminAccess.RoleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(AdminAccess.RoleName));
            ThrowIfFailed(roleResult, "create the Admin role");
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new AdminUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            ThrowIfFailed(createResult, "create the seeded administrator");
        }

        if (!user.LockoutEnabled)
        {
            user.LockoutEnabled = true;
            var updateResult = await userManager.UpdateAsync(user);
            ThrowIfFailed(updateResult, "enable administrator lockout protection");
        }

        if (!await userManager.IsInRoleAsync(user, AdminAccess.RoleName))
        {
            var roleResult = await userManager.AddToRoleAsync(user, AdminAccess.RoleName);
            ThrowIfFailed(roleResult, "assign the Admin role");
        }

        logger.LogInformation("Development administrator {Email} is ready.", email);
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Failed to {operation}: {errors}");
    }
}
