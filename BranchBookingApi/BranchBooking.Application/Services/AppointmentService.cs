using BranchBooking.Application.DTOs;
using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Request;
using BranchBooking.Core.Exceptions;

namespace BranchBooking.Application.Services;

public sealed class AppointmentService(IAppointmentRepository repository) : IAppointmentService
{
    private static readonly TimeZoneInfo Sast = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "South Africa Standard Time" : "Africa/Johannesburg");

    public async Task<IEnumerable<CustomerBranchDto>> GetBookableBranchesAsync(CancellationToken cancellationToken = default) =>
        (await repository.GetBookableBranchesAsync(cancellationToken))
        .Select(branch => new CustomerBranchDto(branch.BranchId, branch.Code, branch.Name, branch.City)).ToArray();

    public async Task<IEnumerable<CustomerServiceDto>> GetBookableServicesAsync(long branchId, CancellationToken cancellationToken = default)
    {
        ValidateId(branchId, "Branch ID");
        return (await repository.GetBookableServicesAsync(branchId, cancellationToken))
            .Select(service => new CustomerServiceDto(service.ServiceId, service.Code, service.Name, service.DurationMinutes)).ToArray();
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(long customerId, long branchId, long serviceId, DateOnly date, long? excludingAppointmentId = null, CancellationToken cancellationToken = default)
    {
        ValidateId(customerId, "Customer ID");
        ValidateId(branchId, "Branch ID");
        ValidateId(serviceId, "Service ID");
        ValidateDate(date);
        return (await repository.GetAvailableSlotsAsync(customerId, branchId, serviceId, date, excludingAppointmentId, cancellationToken))
            .Select(slot => new AvailableSlotDto(slot.StartAt, slot.EndAt)).ToArray();
    }

    public async Task<IEnumerable<AppointmentDto>> GetCustomerAppointmentsAsync(long customerId, bool history, CancellationToken cancellationToken = default) =>
        (await repository.GetCustomerAppointmentsAsync(customerId, history, cancellationToken))
        .Select(appointment => new AppointmentDto(appointment.AppointmentId, appointment.BranchId, appointment.BranchName, appointment.ServiceId, appointment.ServiceName, appointment.StartAt, appointment.EndAt, appointment.Status)).ToArray();

    public async Task<PagedOperationalAppointmentDto> GetOperationalAppointmentsAsync(long? branchId, bool history, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (branchId is <= 0) throw new DomainValidationException("Branch ID must be greater than zero.");
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var records = (await repository.GetOperationalAppointmentsAsync(branchId, history, pageNumber, pageSize, cancellationToken)).ToArray();
        var totalCount = records.FirstOrDefault()?.TotalCount ?? 0;
        return new PagedOperationalAppointmentDto(
            records.Select(appointment => new OperationalAppointmentDto(
                appointment.AppointmentId,
                appointment.CustomerId,
                appointment.CustomerName,
                appointment.CustomerEmail,
                appointment.BranchId,
                appointment.BranchName,
                appointment.ServiceId,
                appointment.ServiceName,
                appointment.StartAt,
                appointment.EndAt,
                appointment.Status)).ToArray(),
            pageNumber,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<long> CreateAsync(long customerId, CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateId(customerId, "Customer ID");
        ArgumentNullException.ThrowIfNull(request);
        ValidateDateTime(request.StartAt);
        var date = DateOnly.FromDateTime(request.StartAt);
        var slots = await repository.GetAvailableSlotsAsync(customerId, request.BranchId, request.ServiceId, date, null, cancellationToken);
        if (!slots.Any(slot => slot.StartAt == request.StartAt)) throw new DomainValidationException("The selected appointment slot is no longer available.");
        return await repository.CreateAsync(customerId, request.BranchId, request.ServiceId, request.StartAt, cancellationToken);
    }

    public async Task CancelAsync(long customerId, long appointmentId, CancellationToken cancellationToken = default)
    {
        ValidateId(appointmentId, "Appointment ID");
        if (!await repository.CancelAsync(customerId, appointmentId, cancellationToken)) throw new DomainValidationException("The appointment could not be cancelled.");
    }

    public async Task RescheduleAsync(long customerId, long appointmentId, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateId(appointmentId, "Appointment ID");
        ArgumentNullException.ThrowIfNull(request);
        ValidateDateTime(request.StartAt);
        if (!await repository.RescheduleAsync(customerId, appointmentId, request.StartAt, cancellationToken)) throw new DomainValidationException("The selected appointment slot is no longer available.");
    }

    private static void ValidateId(long value, string name) { if (value <= 0) throw new DomainValidationException($"{name} must be greater than zero."); }
    private static void ValidateDate(DateOnly date) { if (date < DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Sast).DateTime)) throw new DomainValidationException("Past dates cannot be booked."); }
    private static void ValidateDateTime(DateTime value) { if (value.Minute % 15 != 0 || value.Second != 0 || value.Kind == DateTimeKind.Utc) throw new DomainValidationException("Appointments must use 15-minute local SAST slots."); ValidateDate(DateOnly.FromDateTime(value)); if (value <= TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Sast).DateTime) throw new DomainValidationException("The appointment must be in the future."); }
}
