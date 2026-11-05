using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Core.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace BranchBooking.Api.Configuration
{
    public static class SecuritySetUp
    {
        public static IServiceCollection SetUpSecurity(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("AuthLogin", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
                options.AddPolicy("AuthRegistration", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    }));
                options.AddPolicy("AuthRefresh", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            });

            var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
            if (!isDevelopment &&
                (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Contains("change-this", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Jwt:SigningKey must be supplied through deployment configuration outside Development.");
            }
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

                            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                            var cacheKey = $"auth-user:{userId}";
                            var user = await cache.GetOrCreateAsync(cacheKey, async entry =>
                            {
                                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                                var repository = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationRepository>();
                                return await repository.GetUserByIdAsync(userId, context.HttpContext.RequestAborted);
                            });
                            if (user is null || user.Status is UserStatus.Disabled or UserStatus.Locked || user.AuthorizationVersion != tokenVersion)
                            {
                                context.Fail("The access token is no longer valid.");
                            }
                        }
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Admin", "Staff", "BranchManager"));
                options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
            });

            return services;
        }
    }
}
