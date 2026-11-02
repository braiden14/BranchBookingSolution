using BranchBooking.Application.Interfaces.Repositories;
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

            return services;
        }
    }
}
