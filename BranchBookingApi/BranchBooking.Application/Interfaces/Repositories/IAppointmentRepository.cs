using BranchBooking.Core.Models.Appointment;

namespace BranchBooking.Application.Interfaces.Repositories;

public interface IAppointmentRepository
{
    Task<IEnumerable<BookableBranchRecord>> GetBookableBranchesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BookableServiceRecord>> GetBookableServicesAsync(long branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AvailableSlotRecord>> GetAvailableSlotsAsync(long customerId, long branchId, long serviceId, DateOnly date, long? excludingAppointmentId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AppointmentRecord>> GetCustomerAppointmentsAsync(long customerId, bool history, CancellationToken cancellationToken = default);
    Task<IEnumerable<OperationalAppointmentRecord>> GetOperationalAppointmentsAsync(long? branchId, bool history, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<long> CreateAsync(long customerId, long branchId, long serviceId, DateTime startAt, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(long customerId, long appointmentId, CancellationToken cancellationToken = default);
    Task<bool> RescheduleAsync(long customerId, long appointmentId, DateTime startAt, CancellationToken cancellationToken = default);
}
