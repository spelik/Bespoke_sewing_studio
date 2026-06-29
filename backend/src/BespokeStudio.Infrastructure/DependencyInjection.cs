using BespokeStudio.Application.Abstractions;
using BespokeStudio.Infrastructure.Authentication;
using BespokeStudio.Infrastructure.Notifications;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using BespokeStudio.Infrastructure.Storage;
using BespokeStudio.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
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

        services.AddDataProtection();

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
            .AddOptions<UploadSecurityOptions>()
            .Bind(configuration.GetSection(UploadSecurityOptions.SectionName))
            .Validate(options => IsSupportedMalwareScannerProvider(options.MalwareScanner.Provider), "UploadSecurity:MalwareScanner:Provider must be Disabled, ClamAV or CommandLine.")
            .Validate(options => options.MalwareScanner.TimeoutSeconds is >= 1 and <= 300, "UploadSecurity:MalwareScanner:TimeoutSeconds must be between 1 and 300.")
            .Validate(options => IsScannerExecutableConfigured(options), "UploadSecurity:MalwareScanner:ExecutablePath is required when scanning is enabled.")
            .ValidateOnStart();

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

        services.AddScoped<IContactMessageService, ContactMessageService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IServiceOfferingService, ServiceOfferingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IPageContentService, PageContentService>();
        services.AddScoped<IRepeatableContentService, RepeatableContentService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<IEmailDeliverySettingsService, EmailDeliverySettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<LoggingEmailNotificationSender>();
        services.AddScoped<SmtpEmailNotificationSender>();
        services.AddScoped<IEmailNotificationSender, ConfiguredEmailNotificationSender>();
        services.AddScoped<IMalwareScanner, ConfiguredMalwareScanner>();
        services.AddScoped<IUploadService, LocalUploadService>();
        services.AddScoped<IUploadCleanupService, UploadCleanupService>();

        return services;
    }

    private static bool IsSupportedMalwareScannerProvider(string provider) =>
        string.Equals(provider, "Disabled", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "ClamAV", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "CommandLine", StringComparison.OrdinalIgnoreCase);

    private static bool IsScannerExecutableConfigured(UploadSecurityOptions options) =>
        string.Equals(options.MalwareScanner.Provider, "Disabled", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(options.MalwareScanner.ExecutablePath);
}
