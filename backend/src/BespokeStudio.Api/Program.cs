using System.Text;
using System.Text.Json.Serialization;
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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>() ?? new CorsSettings();

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
app.MapAuthEndpoints();

app.Run();
