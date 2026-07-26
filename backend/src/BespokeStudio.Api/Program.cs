using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BespokeStudio.Api.Caching;
using BespokeStudio.Api.Configuration;
using BespokeStudio.Api.Endpoints;
using BespokeStudio.Api.HealthChecks;
using BespokeStudio.Api.Hubs;
using BespokeStudio.Api.Middleware;
using BespokeStudio.Api.Services;
using BespokeStudio.Api.Versioning;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts;
using BespokeStudio.Application.DependencyInjection;
using BespokeStudio.Application.Security;
using BespokeStudio.Infrastructure.Authentication;
using BespokeStudio.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.Logging.AddFilter<EventLogLoggerProvider>(
        "Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService",
        LogLevel.None);
}

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId);

var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>() ?? new CorsSettings();
var uploadStorageSettings = builder.Configuration
    .GetSection(BespokeStudio.Infrastructure.Storage.UploadStorageOptions.SectionName)
    .Get<BespokeStudio.Infrastructure.Storage.UploadStorageOptions>()
    ?? new BespokeStudio.Infrastructure.Storage.UploadStorageOptions();
var rateLimitingSettings = builder.Configuration
    .GetSection(RateLimitingSettings.SectionName)
    .Get<RateLimitingSettings>() ?? new RateLimitingSettings();
var forwardedHeadersSettings = builder.Configuration
    .GetSection(ForwardedHeadersSettings.SectionName)
    .Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();
var dataProtectionSettings = builder.Configuration
    .GetSection(DataProtectionSettings.SectionName)
    .Get<DataProtectionSettings>() ?? new DataProtectionSettings();
var securityHeadersSettings = builder.Configuration
    .GetSection(SecurityHeadersSettings.SectionName)
    .Get<SecurityHeadersSettings>() ?? new SecurityHeadersSettings();

ValidateForwardedHeadersSettings(forwardedHeadersSettings);
ConfigureDataProtection(builder, dataProtectionSettings);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    });
builder.Services.AddOutputCache(options =>
    options.AddPolicy(
        PublicOutputCachePolicy.Name,
        policy => policy.Expire(PublicOutputCachePolicy.Duration)));
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live", "ready"])
    .AddCheck<PostgreSqlHealthCheck>(
        "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = forwardedHeadersSettings.ForwardLimit;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();

    foreach (var proxy in forwardedHeadersSettings.KnownProxies)
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }

    foreach (var network in forwardedHeadersSettings.KnownNetworks)
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<IAdminRealtimeNotifier, SignalRAdminRealtimeNotifier>();
builder.Services.AddSingleton(new ApiVersionInfoProvider(
    builder.Environment.EnvironmentName,
    DateTimeOffset.UtcNow));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit =
        uploadStorageSettings.MaxFileSizeBytes * uploadStorageSettings.MaxFilesPerRequest + 1024 * 1024);
builder.Services
    .AddOptions<RateLimitingSettings>()
    .Bind(builder.Configuration.GetSection(RateLimitingSettings.SectionName))
    .Validate(settings => settings.PublicUploadPermitLimit > 0, "RateLimiting:PublicUploadPermitLimit must be positive.")
    .Validate(settings => settings.PublicOrderPermitLimit > 0, "RateLimiting:PublicOrderPermitLimit must be positive.")
    .Validate(settings => settings.PublicContactPermitLimit > 0, "RateLimiting:PublicContactPermitLimit must be positive.")
    .Validate(settings => settings.WindowMinutes is >= 1 and <= 1440, "RateLimiting:WindowMinutes must be between 1 and 1440.")
    .Validate(settings => settings.AuthLoginPermitLimit > 0, "RateLimiting:AuthLoginPermitLimit must be positive.")
    .Validate(settings => settings.AuthLoginWindowMinutes is >= 1 and <= 1440, "RateLimiting:AuthLoginWindowMinutes must be between 1 and 1440.")
    .Validate(settings => settings.AuthTwoFactorPermitLimit > 0, "RateLimiting:AuthTwoFactorPermitLimit must be positive.")
    .Validate(settings => settings.AuthTwoFactorWindowMinutes is >= 1 and <= 1440, "RateLimiting:AuthTwoFactorWindowMinutes must be between 1 and 1440.")
    .ValidateOnStart();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
            ? value
            : TimeSpan.FromMinutes(rateLimitingSettings.WindowMinutes);
        context.HttpContext.Response.Headers.RetryAfter =
            Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("PublicRateLimiting");
        logger.LogWarning(
            "Rate limit exceeded for {RemoteIpAddress} on {RequestPath}.",
            context.HttpContext.Connection.RemoteIpAddress,
            context.HttpContext.Request.Path);

        await Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too many requests",
                detail: "Too many requests were submitted. Please wait before trying again.")
            .ExecuteAsync(context.HttpContext);
    };

    options.AddPolicy(RateLimitPolicies.PublicUpload, context =>
        CreateFixedWindowPartition(
            context,
            rateLimitingSettings.PublicUploadPermitLimit,
            rateLimitingSettings.WindowMinutes,
            RateLimitPolicies.PublicUpload));
    options.AddPolicy(RateLimitPolicies.PublicOrder, context =>
        CreateFixedWindowPartition(
            context,
            rateLimitingSettings.PublicOrderPermitLimit,
            rateLimitingSettings.WindowMinutes,
            RateLimitPolicies.PublicOrder));
    options.AddPolicy(RateLimitPolicies.PublicContact, context =>
        CreateFixedWindowPartition(
            context,
            rateLimitingSettings.PublicContactPermitLimit,
            rateLimitingSettings.WindowMinutes,
            RateLimitPolicies.PublicContact));
    options.AddPolicy(RateLimitPolicies.AuthLogin, context =>
        CreateFixedWindowPartition(
            context,
            rateLimitingSettings.AuthLoginPermitLimit,
            rateLimitingSettings.AuthLoginWindowMinutes,
            RateLimitPolicies.AuthLogin));
    options.AddPolicy(RateLimitPolicies.AuthTwoFactor, context =>
        CreateFixedWindowPartition(
            context,
            rateLimitingSettings.AuthTwoFactorPermitLimit,
            rateLimitingSettings.AuthTwoFactorWindowMinutes,
            RateLimitPolicies.AuthTwoFactor));
});

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer), "Jwt:Issuer is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), "Jwt:Audience is required.")
    .Validate(settings => settings.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
    .Validate(settings => settings.AccessTokenMinutes is >= 5 and <= 60, "Jwt:AccessTokenMinutes must be between 5 and 60.")
    .ValidateOnStart();
builder.Services
    .AddOptions<RefreshTokenSettings>()
    .Bind(builder.Configuration.GetSection(RefreshTokenSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.CookieName), "RefreshToken:CookieName is required.")
    .Validate(settings => settings.LifetimeDays is >= 1 and <= 90, "RefreshToken:LifetimeDays must be between 1 and 90.")
    .ValidateOnStart();

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    path.StartsWithSegments("/hubs/admin-notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var userIdValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenSecurityStamp = principal?.FindFirstValue(AdminAccess.SecurityStampClaimType);
                if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(tokenSecurityStamp))
                {
                    context.Fail("The access token is no longer valid.");
                    return;
                }

                try
                {
                    var userManager = context.HttpContext.RequestServices
                        .GetRequiredService<UserManager<AdminUser>>();
                    var user = await userManager.FindByIdAsync(userId.ToString());
                    if (user is null ||
                        await userManager.IsLockedOutAsync(user) ||
                        !await userManager.IsInRoleAsync(user, AdminAccess.RoleName))
                    {
                        context.Fail("The access token is no longer valid.");
                        return;
                    }

                    var currentSecurityStamp = await userManager.GetSecurityStampAsync(user);
                    if (!string.Equals(tokenSecurityStamp, currentSecurityStamp, StringComparison.Ordinal))
                    {
                        context.Fail("The access token is no longer valid.");
                    }
                }
                catch (Exception exception)
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("JwtUserValidation");
                    logger.LogWarning(exception, "JWT user validation failed closed.");
                    context.Fail("The access token could not be validated.");
                }
            }
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminAccess.PolicyName, policy => policy.RequireRole(AdminAccess.RoleName));
builder.Services.AddScoped<IAuthService, JwtAuthService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsSettings.PolicyName, policy =>
    {
        if (corsSettings.AllowedOrigins.Count == 0)
        {
            return;
        }

        policy
            .WithOrigins(corsSettings.AllowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.SeedAdminIdentityAsync(app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

app.UseMiddleware<CorrelationIdMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Frame-Options"] = "DENY";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        if (securityHeadersSettings.EnableContentSecurityPolicy &&
            !string.IsNullOrWhiteSpace(securityHeadersSettings.ContentSecurityPolicy))
        {
            headers["Content-Security-Policy"] = securityHeadersSettings.ContentSecurityPolicy;
        }

        return Task.CompletedTask;
    });

    await next();
});

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(CorsSettings.PolicyName);
app.UseAuthentication();
app.UseOutputCache();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHub<AdminNotificationsHub>("/hubs/admin-notifications")
    .RequireAuthorization(AdminAccess.PolicyName);

app.MapHealthChecks("/health", CreateHealthCheckOptions("live"));
app.MapHealthChecks("/health/live", CreateHealthCheckOptions("live"));
app.MapHealthChecks("/health/ready", CreateHealthCheckOptions("ready"));
app.MapHealthChecks("/healthz", CreateHealthCheckOptions("live"));
app.MapHealthChecks("/readyz", CreateHealthCheckOptions("ready"));

var api = app.MapGroup("/api")
    .WithTags("System");

api.MapGet("/health", async (HealthCheckService healthCheckService, CancellationToken cancellationToken) =>
{
    var report = await healthCheckService.CheckHealthAsync(
        registration => registration.Tags.Contains("live"),
        cancellationToken);
    var status = report.Status == HealthStatus.Healthy ? "ok" : "degraded";

    return TypedResults.Ok(new ApiHealthResponse(
        Status: status,
        Application: "Bespoke Sewing Studio API"));
})
.WithName("GetApiHealth");

api.MapGet("/version", (ApiVersionInfoProvider versionInfoProvider) =>
    TypedResults.Ok(versionInfoProvider.GetVersionInfo()))
    .WithName("GetApiVersion");

app.MapOrderEndpoints();
app.MapContactMessageEndpoints();
app.MapAuthEndpoints();
app.MapAdminUserEndpoints();
app.MapAdminAuditLogEndpoints();
app.MapStorageMaintenanceEndpoints();
app.MapSiteSettingsEndpoints();
app.MapNotificationEndpoints();
app.MapEmailDeliverySettingsEndpoints();
app.MapEmailDeliveryLogEndpoints();
app.MapProductionReadinessEndpoints();
app.MapServiceOfferingEndpoints();
app.MapPortfolioEndpoints();
app.MapInStockEndpoints();
app.MapContentEndpoints();
app.MapRepeatableContentEndpoints();
app.MapBrandSettingsEndpoints();
app.MapUploadEndpoints(uploadStorageSettings.PublicBasePath);

app.MapFallback(context => ServeSpaFallbackAsync(context, app.Environment));

app.Run();

static RateLimitPartition<string> CreateFixedWindowPartition(
    HttpContext context,
    int permitLimit,
    int windowMinutes,
    string policyName)
{
    var remoteAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: $"{policyName}:{remoteAddress}",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(windowMinutes),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
}

static HealthCheckOptions CreateHealthCheckOptions(string tag) => new()
{
    Predicate = registration => registration.Tags.Contains(tag),
    ResponseWriter = WriteHealthResponseAsync,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
};

static async Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        status = report.Status.ToString()
    }));
}

static async Task ServeSpaFallbackAsync(HttpContext context, IWebHostEnvironment environment)
{
    if (IsBackendRoute(context.Request.Path))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var webRootPath = string.IsNullOrWhiteSpace(environment.WebRootPath)
        ? Path.Combine(environment.ContentRootPath, "wwwroot")
        : environment.WebRootPath;
    var indexPath = Path.Combine(webRootPath, "index.html");

    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
}

static bool IsBackendRoute(PathString path) =>
    path.StartsWithSegments("/api") ||
    path.StartsWithSegments("/health") ||
    path.StartsWithSegments("/healthz") ||
    path.StartsWithSegments("/readyz") ||
    path.StartsWithSegments("/hubs") ||
    path.StartsWithSegments("/swagger");

static void ValidateForwardedHeadersSettings(ForwardedHeadersSettings settings)
{
    if (settings.ForwardLimit is < 1 or > 10)
    {
        throw new InvalidOperationException(
            "ForwardedHeaders:ForwardLimit must be between 1 and 10.");
    }

    if (settings.KnownProxies.Any(proxy => !IPAddress.TryParse(proxy, out _)))
    {
        throw new InvalidOperationException(
            "ForwardedHeaders:KnownProxies contains an invalid IP address.");
    }

    if (settings.KnownNetworks.Any(network => !System.Net.IPNetwork.TryParse(network, out _)))
    {
        throw new InvalidOperationException(
            "ForwardedHeaders:KnownNetworks contains an invalid CIDR network.");
    }
}

static void ConfigureDataProtection(
    WebApplicationBuilder builder,
    DataProtectionSettings settings)
{
    if (string.IsNullOrWhiteSpace(settings.ApplicationName))
    {
        throw new InvalidOperationException(
            "DataProtection:ApplicationName is required.");
    }

    var dataProtection = builder.Services
        .AddDataProtection()
        .SetApplicationName(settings.ApplicationName);

    if (string.IsNullOrWhiteSpace(settings.KeysPath))
    {
        if (builder.Environment.IsProduction())
        {
            throw new InvalidOperationException(
                "DataProtection:KeysPath is required in Production.");
        }

        return;
    }

    var keysPath = Path.IsPathRooted(settings.KeysPath)
        ? settings.KeysPath
        : Path.GetFullPath(settings.KeysPath, builder.Environment.ContentRootPath);

    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
