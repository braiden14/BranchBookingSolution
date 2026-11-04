using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Infrastructure.Authentication;
using BranchBooking.Infrastructure.Persistence;
using BranchBooking.Infrastructure.Persistence.Repositories;

namespace BranchBooking.Api.Configuration
{
    public static class InfrastructureSetUp
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("BookingDatabase");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'BookingDatabase' is not configured.");
            }

            services.AddSingleton(new DbConnectionFactory(connectionString));
            services.AddSingleton<IBranchRepository, BranchRepository>();
            services.AddSingleton<IOperationalScheduleRepository, OperationalScheduleRepository>();
            services.AddSingleton<IBranchConfigurationRepository, BranchConfigurationRepository>();
            services.AddSingleton<IAppointmentRepository, AppointmentRepository>();
            services.AddSingleton<ICustomerRepository, CustomerRepository>();
            services.AddSingleton<IAuthenticationRepository, AuthenticationRepository>();
            services.AddSingleton<IPasswordService, PasswordService>();
            services.AddSingleton<BootstrapAdminSeeder>();

            var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
            if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
            {
                throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 characters.");
            }

            services.AddSingleton(jwtOptions);
            services.AddSingleton<ITokenService, JwtTokenService>();

            return services;
        }
    }
}
