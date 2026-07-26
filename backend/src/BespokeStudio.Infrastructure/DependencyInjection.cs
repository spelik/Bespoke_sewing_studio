using BespokeStudio.Application.Abstractions;
using BespokeStudio.Infrastructure.Authentication;
using BespokeStudio.Infrastructure.Notifications;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using BespokeStudio.Infrastructure.Storage;
using BespokeStudio.Infrastructure.Security;
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
            .AddOptions<EmailOutboxOptions>()
            .Bind(configuration.GetSection(EmailOutboxOptions.SectionName))
            .Validate(options => options.WorkerIntervalSeconds is >= 5 and <= 3600, "EmailOutbox:WorkerIntervalSeconds must be between 5 and 3600.")
            .Validate(options => options.BatchSize is >= 1 and <= 200, "EmailOutbox:BatchSize must be between 1 and 200.")
            .Validate(options => options.MaxAttempts is >= 1 and <= 20, "EmailOutbox:MaxAttempts must be between 1 and 20.")
            .Validate(options => options.RetryBaseSeconds is >= 1 and <= 86400, "EmailOutbox:RetryBaseSeconds must be between 1 and 86400.")
            .Validate(options => options.RetryMaxMinutes is >= 1 and <= 1440, "EmailOutbox:RetryMaxMinutes must be between 1 and 1440.")
            .Validate(options => options.ProcessingTimeoutMinutes is >= 1 and <= 1440, "EmailOutbox:ProcessingTimeoutMinutes must be between 1 and 1440.")
            .ValidateOnStart();

        services
            .AddOptions<EmailOutboxRetentionOptions>()
            .Bind(configuration.GetSection(EmailOutboxRetentionOptions.SectionName))
            .Validate(options => options.WorkerIntervalHours is >= 1 and <= 168, "EmailOutboxRetention:WorkerIntervalHours must be between 1 and 168.")
            .Validate(options => options.BatchSize is >= 1 and <= 1000, "EmailOutboxRetention:BatchSize must be between 1 and 1000.")
            .Validate(options => options.SucceededBodyRetentionDays is >= 1 and <= 3650, "EmailOutboxRetention:SucceededBodyRetentionDays must be between 1 and 3650.")
            .Validate(options => options.SucceededMessageRetentionDays is >= 1 and <= 3650, "EmailOutboxRetention:SucceededMessageRetentionDays must be between 1 and 3650.")
            .Validate(options => options.SkippedBodyRetentionDays is >= 1 and <= 3650, "EmailOutboxRetention:SkippedBodyRetentionDays must be between 1 and 3650.")
            .Validate(options => options.SkippedMessageRetentionDays is >= 1 and <= 3650, "EmailOutboxRetention:SkippedMessageRetentionDays must be between 1 and 3650.")
            .Validate(options => options.SucceededMessageRetentionDays >= options.SucceededBodyRetentionDays, "EmailOutboxRetention:SucceededMessageRetentionDays must be greater than or equal to SucceededBodyRetentionDays.")
            .Validate(options => options.SkippedMessageRetentionDays >= options.SkippedBodyRetentionDays, "EmailOutboxRetention:SkippedMessageRetentionDays must be greater than or equal to SkippedBodyRetentionDays.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.PurgedBodyPlaceholder)
                    && options.PurgedBodyPlaceholder.Length <= 500,
                "EmailOutboxRetention:PurgedBodyPlaceholder is required and must be at most 500 characters.")
            .ValidateOnStart();

        services
            .AddOptions<UploadSecurityOptions>()
            .Bind(configuration.GetSection(UploadSecurityOptions.SectionName))
            .Validate(options => IsSupportedMalwareScannerProvider(options.MalwareScanner.Provider), "UploadSecurity:MalwareScanner:Provider must be Disabled, ClamAV or CommandLine.")
            .Validate(options => options.MalwareScanner.TimeoutSeconds is >= 1 and <= 300, "UploadSecurity:MalwareScanner:TimeoutSeconds must be between 1 and 300.")
            .Validate(options => options.MalwareScanner.ClamAv.Port is >= 1 and <= 65535, "UploadSecurity:MalwareScanner:ClamAv:Port must be between 1 and 65535.")
            .Validate(options => options.MalwareScanner.ClamAv.MaxChunkSizeBytes is >= 1024 and <= 1048576, "UploadSecurity:MalwareScanner:ClamAv:MaxChunkSizeBytes must be between 1024 and 1048576.")
            .Validate(options => IsClamAvEndpointConfigured(options), "UploadSecurity:MalwareScanner:ClamAv:Host is required when Provider is ClamAV.")
            .Validate(options => IsScannerExecutableConfigured(options), "UploadSecurity:MalwareScanner:ExecutablePath is required when scanning is enabled.")
            .ValidateOnStart();

        services
            .AddOptions<UploadDeletionOptions>()
            .Bind(configuration.GetSection(UploadDeletionOptions.SectionName))
            .Validate(options => options.PollIntervalSeconds is >= 5 and <= 3600, "UploadDeletion:PollIntervalSeconds must be between 5 and 3600.")
            .Validate(options => options.BatchSize is >= 1 and <= 200, "UploadDeletion:BatchSize must be between 1 and 200.")
            .Validate(options => options.MaxAttempts is >= 1 and <= 20, "UploadDeletion:MaxAttempts must be between 1 and 20.")
            .Validate(options => options.BaseRetrySeconds is >= 5 and <= 86400, "UploadDeletion:BaseRetrySeconds must be between 5 and 86400.")
            .Validate(options => options.ProcessingTimeoutMinutes is >= 1 and <= 1440, "UploadDeletion:ProcessingTimeoutMinutes must be between 1 and 1440.")
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
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<BespokeStudioDbContext>();

        services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
        services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();
        services.AddScoped<IContactMessageService, ContactMessageService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IServiceOfferingService, ServiceOfferingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IDbContextTransactionFactory, BespokeStudioDbContextTransactionFactory>();
        services.AddScoped<IInStockService, InStockService>();
        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<IPageContentService, PageContentService>();
        services.AddScoped<IRepeatableContentService, RepeatableContentService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<IEmailDeliverySettingsService, EmailDeliverySettingsService>();
        services.AddScoped<IEmailDeliveryLogService, EmailDeliveryLogService>();
        services.AddScoped<IEmailOutboxService, EmailOutboxService>();
        services.AddScoped<IEmailOutboxRetentionService, EmailOutboxRetentionService>();
        services.AddScoped<IProductionReadinessService, ProductionReadinessService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<LoggingEmailNotificationSender>();
        services.AddScoped<SmtpEmailNotificationSender>();
        services.AddHttpClient<ResendEmailNotificationSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient("DnsOverHttps", client =>
        {
            client.BaseAddress = new Uri("https://cloudflare-dns.com/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/dns-json");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<IEmailNotificationSender, ConfiguredEmailNotificationSender>();
        services.AddScoped<IMalwareScanner, ConfiguredMalwareScanner>();
        services.AddScoped<IUploadStorage, LocalUploadStorage>();
        services.AddScoped<IUploadService, LocalUploadService>();
        services.AddScoped<IUploadCleanupService, UploadCleanupService>();
        services.AddScoped<IStorageMaintenanceService, StorageMaintenanceService>();
        services.AddScoped<IUploadFileDeletionScheduler, UploadFileDeletionScheduler>();
        services.AddScoped<IUploadFileDeletionProcessor, UploadFileDeletionProcessor>();
        services.AddHostedService<UploadFileDeletionWorker>();
        services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();
        services.AddHostedService<EmailOutboxWorker>();
        services.AddHostedService<EmailOutboxRetentionWorker>();

        return services;
    }

    private static bool IsSupportedMalwareScannerProvider(string provider) =>
        string.Equals(provider, "Disabled", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "ClamAV", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "CommandLine", StringComparison.OrdinalIgnoreCase);

    private static bool IsScannerExecutableConfigured(UploadSecurityOptions options) =>
        string.Equals(options.MalwareScanner.Provider, "Disabled", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(options.MalwareScanner.Provider, "ClamAV", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(options.MalwareScanner.ExecutablePath);

    private static bool IsClamAvEndpointConfigured(UploadSecurityOptions options) =>
        !string.Equals(options.MalwareScanner.Provider, "ClamAV", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(options.MalwareScanner.ClamAv.Host);
}
