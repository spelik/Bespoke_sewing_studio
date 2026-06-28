using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BespokeStudio.Api.Configuration;
using BespokeStudio.Api.Endpoints;
using BespokeStudio.Api.Services;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts;
using BespokeStudio.Application.DependencyInjection;
using BespokeStudio.Application.Security;
using BespokeStudio.Infrastructure.Authentication;
using BespokeStudio.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks();
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
});

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer), "Jwt:Issuer is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), "Jwt:Audience is required.")
    .Validate(settings => settings.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters.")
    .Validate(settings => settings.ExpirationHours is >= 1 and <= 24, "Jwt:ExpirationHours must be between 1 and 24.")
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
            .AllowAnyMethod();
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsSettings.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

var api = app.MapGroup("/api")
    .WithTags("System");

api.MapGet("/health", async (HealthCheckService healthCheckService, CancellationToken cancellationToken) =>
{
    var report = await healthCheckService.CheckHealthAsync(cancellationToken);
    var status = report.Status == HealthStatus.Healthy ? "ok" : "degraded";

    return TypedResults.Ok(new ApiHealthResponse(
        Status: status,
        Application: "Bespoke Sewing Studio API"));
})
.WithName("GetApiHealth");

api.MapGet("/version", () =>
    TypedResults.Ok(new ApiVersionResponse(
        Name: "Bespoke Sewing Studio API",
        Mode: "skeleton")))
    .WithName("GetApiVersion");

app.MapOrderEndpoints();
app.MapContactMessageEndpoints();
app.MapAuthEndpoints();
app.MapSiteSettingsEndpoints();
app.MapNotificationEndpoints();
app.MapServiceOfferingEndpoints();
app.MapPortfolioEndpoints();
app.MapContentEndpoints();
app.MapRepeatableContentEndpoints();
app.MapBrandSettingsEndpoints();
app.MapUploadEndpoints(uploadStorageSettings.PublicBasePath);

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
