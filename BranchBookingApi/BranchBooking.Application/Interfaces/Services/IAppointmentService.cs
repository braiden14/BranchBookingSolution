using BranchBooking.Application.DTOs;
using BranchBooking.Application.Models.Request;

namespace BranchBooking.Application.Interfaces.Services;

public interface IAppointmentService
{
    Task<IEnumerable<CustomerBranchDto>> GetBookableBranchesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerServiceDto>> GetBookableServicesAsync(long branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(long branchId, long serviceId, DateOnly date, long? excludingAppointmentId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsAsync(long customerId, bool history, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(long customerId, CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task CancelAsync(long customerId, long appointmentId, CancellationToken cancellationToken = default);
    Task RescheduleAsync(long customerId, long appointmentId, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);
}
