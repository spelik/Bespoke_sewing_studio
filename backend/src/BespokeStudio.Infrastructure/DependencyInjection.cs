using BespokeStudio.Application.Abstractions;
using BespokeStudio.Infrastructure.Authentication;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BespokeStudio.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BespokeStudioDb")
            ?? throw new InvalidOperationException(
                "Connection string 'BespokeStudioDb' is not configured.");

        services.AddDbContext<BespokeStudioDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(BespokeStudioDbContext).Assembly.FullName)));

        services
            .AddIdentityCore<AdminUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<BespokeStudioDbContext>();

        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
