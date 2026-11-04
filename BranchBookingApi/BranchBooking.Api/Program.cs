using BranchBooking.Api.Configuration;
using BranchBooking.Api.Endpoints;
using BranchBooking.Api.Middleware;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Core.Enums;
using BranchBooking.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Serilog;

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
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Contains("change-this", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Jwt:SigningKey must be supplied through deployment configuration outside Development.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var versionValue = context.Principal?.FindFirst("authorization_version")?.Value;
                if (!long.TryParse(subject, out var userId) || !int.TryParse(versionValue, out var tokenVersion))
                {
                    context.Fail("The access token is invalid.");
                    return;
                }

                var repository = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationRepository>();
                var user = await repository.GetUserByIdAsync(userId, context.HttpContext.RequestAborted);
                if (user is null || user.Status is UserStatus.Disabled or UserStatus.Locked || user.AuthorizationVersion != tokenVersion)
                {
                    context.Fail("The access token is no longer valid.");
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Admin", "Staff", "BranchManager"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

var bootstrapAdminOptions = app.Configuration.GetSection("BootstrapAdmin").Get<BootstrapAdminOptions>() ?? new BootstrapAdminOptions();
if (bootstrapAdminOptions.Enabled)
{
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
