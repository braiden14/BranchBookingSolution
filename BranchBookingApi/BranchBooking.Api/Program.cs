using BranchBooking.Api.Configuration;
using BranchBooking.Api.Endpoints;
using BranchBooking.Api.Middleware;
using BranchBooking.Infrastructure.Authentication;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using Serilog;
using OpenTelemetry.Metrics;
using BranchBooking.Api.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(ApiTelemetry.MeterName);

        var exporter = builder.Configuration["OpenTelemetry:Metrics:Exporter"];
        if (string.Equals(exporter, "otlp", StringComparison.OrdinalIgnoreCase))
        {
            metrics.AddOtlpExporter(options =>
            {
                var endpoint = builder.Configuration["OpenTelemetry:Metrics:OtlpEndpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    options.Endpoint = new Uri(endpoint);
                }
            });
        }
        else if (string.Equals(exporter, "console", StringComparison.OrdinalIgnoreCase))
        {
            metrics.AddConsoleExporter();
        }
    });

builder.Services.SetUpSecurity(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

app.UseRouting();
app.UseMiddleware<ApiTelemetryMiddleware>();

app.Logger.LogInformation(
    "BranchBooking API starting in {EnvironmentName} environment with metrics exporter {MetricsExporter}",
    app.Environment.EnvironmentName,
    app.Configuration["OpenTelemetry:Metrics:Exporter"] ?? "disabled");

var bootstrapAdminOptions = app.Configuration.GetSection("BootstrapAdmin").Get<BootstrapAdminOptions>() ?? new BootstrapAdminOptions();
if (bootstrapAdminOptions.Enabled)
{
    app.Logger.LogInformation("Bootstrap administrator seeding is enabled");
    using var scope = app.Services.CreateScope();
    var bootstrapAdminSeeder = scope.ServiceProvider.GetRequiredService<BootstrapAdminSeeder>();
    await bootstrapAdminSeeder.SeedAsync(bootstrapAdminOptions);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapBranchEndpoints();
app.MapAuthenticationEndpoints();
app.MapOperationalScheduleEndpoints();
app.MapBranchConfigurationEndpoints();
app.MapAppointmentEndpoints();
app.MapCustomerEndpoints();


app.Run();

public partial class Program
{
}
