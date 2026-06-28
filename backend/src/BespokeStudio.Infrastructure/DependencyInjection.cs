using BespokeStudio.Application.Abstractions;
using BespokeStudio.Infrastructure.Authentication;
using BespokeStudio.Infrastructure.Notifications;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using BespokeStudio.Infrastructure.Storage;
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
            .AddOptions<UploadStorageOptions>()
            .Bind(configuration.GetSection(UploadStorageOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.RootPath), "UploadStorage:RootPath is required.")
            .Validate(options => options.PublicBasePath.StartsWith("/api/", StringComparison.Ordinal), "UploadStorage:PublicBasePath must start with /api/.")
            .Validate(options => options.MaxFileSizeBytes > 0, "UploadStorage:MaxFileSizeBytes must be positive.")
            .Validate(options => options.MaxFilesPerRequest is >= 1 and <= 5, "UploadStorage:MaxFilesPerRequest must be between 1 and 5.")
            .Validate(options => options.OrphanCleanupAgeMinutes is >= 1 and <= 10080, "UploadStorage:OrphanCleanupAgeMinutes must be between 1 and 10080.")
            .Validate(options => options.AllowedContentTypes.Count > 0, "UploadStorage:AllowedContentTypes is required.")
            .ValidateOnStart();

        services
            .AddOptions<EmailNotificationOptions>()
            .Bind(configuration.GetSection(EmailNotificationOptions.SectionName));

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
        services.AddScoped<IServiceOfferingService, ServiceOfferingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IPageContentService, PageContentService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<LoggingEmailNotificationSender>();
        services.AddScoped<SmtpEmailNotificationSender>();
        services.AddScoped<IEmailNotificationSender, ConfiguredEmailNotificationSender>();
        services.AddScoped<IUploadService, LocalUploadService>();
        services.AddScoped<IUploadCleanupService, UploadCleanupService>();

        return services;
    }
}
