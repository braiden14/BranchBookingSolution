using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Core.Models.Appointment;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository(DbConnectionFactory connectionFactory) : IAppointmentRepository
{
    private const string BranchesSql = "SELECT * FROM public.get_bookable_branches()";
    private const string ServicesSql = "SELECT * FROM public.get_bookable_services(@BranchId)";
    private const string AppointmentsSql = "SELECT * FROM public.get_customer_appointments(@CustomerId, @History)";
    private const string OperationalAppointmentsSql = "SELECT * FROM public.get_operational_appointments(@BranchId, @History, @PageSize, @Offset)";
    private const string SlotsSql = "SELECT * FROM public.get_available_slots(@CustomerId, @BranchId, @ServiceId, @Date, @ExcludeId, @NowSast)";

    public async Task<IEnumerable<BookableBranchRecord>> GetBookableBranchesAsync(CancellationToken cancellationToken = default) => await QueryAsync<BookableBranchRecord>(BranchesSql, null, cancellationToken);
    public async Task<IEnumerable<BookableServiceRecord>> GetBookableServicesAsync(long branchId, CancellationToken cancellationToken = default) => await QueryAsync<BookableServiceRecord>(ServicesSql, new { BranchId = branchId }, cancellationToken);
    public async Task<IEnumerable<AppointmentRecord>> GetCustomerAppointmentsAsync(long customerId, bool history, CancellationToken cancellationToken = default) => await QueryAsync<AppointmentRecord>(AppointmentsSql, new { CustomerId = customerId, History = history }, cancellationToken);
    public async Task<IEnumerable<OperationalAppointmentRecord>> GetOperationalAppointmentsAsync(long? branchId, bool history, int pageNumber, int pageSize, CancellationToken cancellationToken = default) => await QueryAsync<OperationalAppointmentRecord>(OperationalAppointmentsSql, new { BranchId = branchId, History = history, PageSize = pageSize, Offset = (pageNumber - 1) * pageSize }, cancellationToken);

    public async Task<IEnumerable<AvailableSlotRecord>> GetAvailableSlotsAsync(long customerId, long branchId, long serviceId, DateOnly date, long? excludingAppointmentId = null, CancellationToken cancellationToken = default)
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "South Africa Standard Time" : "Africa/Johannesburg")).DateTime;
        return await QueryAsync<AvailableSlotRecord>(SlotsSql, new { CustomerId = customerId, BranchId = branchId, ServiceId = serviceId, Date = date, ExcludeId = excludingAppointmentId, NowSast = now }, cancellationToken);
    }

    public async Task<long> CreateAsync(long customerId, long branchId, long serviceId, DateTime startAt, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var id = await connection.ExecuteScalarAsync<long?>(new CommandDefinition(
            "SELECT public.create_appointment(@CustomerId, @BranchId, @ServiceId, @StartAt)",
            new { CustomerId = customerId, BranchId = branchId, ServiceId = serviceId, StartAt = startAt },
            cancellationToken: cancellationToken));
        if (id is null) throw new DomainValidationException("You already have an appointment at this branch and time.");
        return id.Value;
    }

    public async Task<bool> CancelAsync(long customerId, long appointmentId, CancellationToken cancellationToken = default) => await ExecuteScalarBoolAsync("SELECT public.cancel_appointment(@CustomerId, @AppointmentId)", new { CustomerId = customerId, AppointmentId = appointmentId }, cancellationToken);
    public async Task<bool> RescheduleAsync(long customerId, long appointmentId, DateTime startAt, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await ExecuteScalarBoolAsync("SELECT public.reschedule_appointment(@CustomerId, @AppointmentId, @StartAt)", new { CustomerId = customerId, AppointmentId = appointmentId, StartAt = startAt }, cancellationToken);
    }

    private async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, commandType: CommandType.Text, cancellationToken: cancellationToken));
    }
    private async Task<bool> ExecuteScalarBoolAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool?>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)) ?? false;
    }
}
