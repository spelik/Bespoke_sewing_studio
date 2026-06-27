using System.Text.Json.Serialization;
using BespokeStudio.Api.Configuration;
using BespokeStudio.Api.Endpoints;
using BespokeStudio.Application.Contracts;
using BespokeStudio.Application.DependencyInjection;
using BespokeStudio.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>() ?? new CorsSettings();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsSettings.PolicyName);

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

app.Run();
