using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Services;

namespace BranchBooking.Api.Configuration
{
    public static class ApplicationSetUp
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IBranchService, BranchService>();
            services.AddSingleton<IOperationalScheduleService, OperationalScheduleService>();
            services.AddSingleton<IBranchConfigurationService, BranchConfigurationService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IBranchAccessService, BranchAccessService>();
            services.AddSingleton<IAppointmentService, AppointmentService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton(TimeProvider.System);

            return services;
        }
    }
}
