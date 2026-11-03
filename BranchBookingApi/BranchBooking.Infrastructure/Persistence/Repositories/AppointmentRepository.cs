using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Appointment;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository(DbConnectionFactory connectionFactory) : IAppointmentRepository
{
    private const string BranchesSql = "SELECT id AS BranchId, code AS Code, name AS Name, city AS City FROM public.branches WHERE status = 1 AND accepts_bookings ORDER BY name, id";
    private const string ServicesSql = "SELECT s.id AS ServiceId, s.code AS Code, s.name AS Name, s.service_duration_minutes AS DurationMinutes FROM public.branch_services bs JOIN public.offered_services s ON s.id = bs.service_id JOIN public.branches b ON b.id = bs.branch_id WHERE bs.branch_id = @BranchId AND bs.is_active AND b.status = 1 AND b.accepts_bookings ORDER BY s.name, s.id";
    private const string AppointmentsSql = "SELECT a.id AS AppointmentId, a.customer_id AS CustomerId, a.branch_id AS BranchId, b.name AS BranchName, a.service_id AS ServiceId, s.name AS ServiceName, a.starts_at_utc AT TIME ZONE 'Africa/Johannesburg' AS StartAt, a.ends_at_utc AT TIME ZONE 'Africa/Johannesburg' AS EndAt, a.status AS Status FROM public.appointments a JOIN public.branches b ON b.id = a.branch_id JOIN public.offered_services s ON s.id = a.service_id WHERE a.customer_id = @CustomerId AND ((@History AND a.starts_at_utc < now()) OR (NOT @History AND a.status = 'Confirmed' AND a.starts_at_utc >= now())) ORDER BY a.starts_at_utc";

    private const string SlotsSql = """
WITH selected_service AS (SELECT s.service_duration_minutes AS duration FROM public.offered_services s JOIN public.branch_services bs ON bs.service_id = s.id WHERE s.id = @ServiceId AND bs.branch_id = @BranchId AND bs.is_active),
selected_schedule AS (SELECT id FROM public.branch_operational_schedules WHERE branch_id = @BranchId AND is_active AND effective_from <= @Date AND (effective_to IS NULL OR effective_to >= @Date) ORDER BY effective_from DESC LIMIT 1),
hours AS (SELECT COALESCE(dh.opens_at, wh.opens_at) AS opens_at, COALESCE(dh.closes_at, wh.closes_at) AS closes_at, COALESCE(co.capacity, dh.capacity, wh.capacity, 1) AS capacity, COALESCE(dover.is_closed, false) AS is_closed FROM selected_schedule ss JOIN public.branch_weekly_operating_hours wh ON wh.schedule_id = ss.id AND wh.day_of_week = EXTRACT(DOW FROM @Date::date)::smallint + 1 LEFT JOIN public.branch_date_overrides dover ON dover.schedule_id = ss.id AND dover.override_date = @Date LEFT JOIN public.branch_date_operating_hours dh ON dh.override_id = dover.override_id LEFT JOIN public.branch_daily_capacity_overrides co ON co.branch_id = @BranchId AND co.override_date = @Date),
candidates AS (SELECT gs AS local_start, gs + make_interval(mins => selected_service.duration) AS local_end, hours.capacity FROM hours CROSS JOIN selected_service CROSS JOIN LATERAL generate_series(@Date::date + hours.opens_at, @Date::date + hours.closes_at - make_interval(mins => selected_service.duration), interval '15 minutes') gs WHERE NOT hours.is_closed AND hours.opens_at < hours.closes_at)
SELECT local_start AS StartAt, local_end AS EndAt FROM candidates c WHERE local_start > (@NowSast::timestamp) AND (SELECT COUNT(*) FROM public.appointments a WHERE a.branch_id = @BranchId AND a.status = 'Confirmed' AND (@ExcludeId IS NULL OR a.id <> @ExcludeId) AND a.starts_at_utc < (c.local_end AT TIME ZONE 'Africa/Johannesburg') AND a.ends_at_utc > (c.local_start AT TIME ZONE 'Africa/Johannesburg')) < c.capacity ORDER BY local_start
""";

    public async Task<IEnumerable<BookableBranchRecord>> GetBookableBranchesAsync(CancellationToken cancellationToken = default) => await QueryAsync<BookableBranchRecord>(BranchesSql, null, cancellationToken);
    public async Task<IEnumerable<BookableServiceRecord>> GetBookableServicesAsync(long branchId, CancellationToken cancellationToken = default) => await QueryAsync<BookableServiceRecord>(ServicesSql, new { BranchId = branchId }, cancellationToken);
    public async Task<IEnumerable<AppointmentRecord>> GetCustomerAppointmentsAsync(long customerId, bool history, CancellationToken cancellationToken = default) => await QueryAsync<AppointmentRecord>(AppointmentsSql, new { CustomerId = customerId, History = history }, cancellationToken);

    public async Task<IEnumerable<AvailableSlotRecord>> GetAvailableSlotsAsync(long branchId, long serviceId, DateOnly date, long? excludingAppointmentId = null, CancellationToken cancellationToken = default)
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "South Africa Standard Time" : "Africa/Johannesburg")).DateTime;
        return await QueryAsync<AvailableSlotRecord>(SlotsSql, new { BranchId = branchId, ServiceId = serviceId, Date = date, ExcludeId = excludingAppointmentId, NowSast = now }, cancellationToken);
    }

    public async Task<long> CreateAsync(long customerId, long branchId, long serviceId, DateTime startAt, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        // PostgreSQL has one-key and (int, int) advisory-lock overloads, not
        // (bigint, bigint). Hash a stable branch/date key into the one-key form.
        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT pg_advisory_xact_lock(hashtextextended(@LockKey, 0))",
            new { LockKey = $"branch:{branchId}:date:{startAt:yyyy-MM-dd}" },
            transaction,
            cancellationToken: cancellationToken));
        var duration = await connection.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT s.service_duration_minutes FROM public.offered_services s JOIN public.branch_services bs ON bs.service_id = s.id JOIN public.branches b ON b.id = bs.branch_id WHERE s.id = @ServiceId AND bs.branch_id = @BranchId AND bs.is_active AND b.status = 1 AND b.accepts_bookings", new { BranchId = branchId, ServiceId = serviceId }, transaction, cancellationToken: cancellationToken));
        if (duration is null) throw new InvalidOperationException("The selected service is not available at this branch.");
        var id = await connection.ExecuteScalarAsync<long?>(new CommandDefinition("INSERT INTO public.appointments(customer_id, branch_id, service_id, starts_at_utc, ends_at_utc) SELECT @CustomerId, @BranchId, @ServiceId, @StartAt AT TIME ZONE 'Africa/Johannesburg', (@StartAt + make_interval(mins => @Duration)) AT TIME ZONE 'Africa/Johannesburg' WHERE NOT EXISTS (SELECT 1 FROM public.appointments WHERE customer_id = @CustomerId AND branch_id = @BranchId AND starts_at_utc = @StartAt AT TIME ZONE 'Africa/Johannesburg' AND status = 'Confirmed') RETURNING id", new { CustomerId = customerId, BranchId = branchId, ServiceId = serviceId, StartAt = startAt, Duration = duration.Value }, transaction, cancellationToken: cancellationToken));
        if (id is null) throw new DomainValidationException("You already have an appointment at this branch and time.");
        await transaction.CommitAsync(cancellationToken);
        return id.Value;
    }

    public async Task<bool> CancelAsync(long customerId, long appointmentId, CancellationToken cancellationToken = default) => await ExecuteBoolAsync("UPDATE public.appointments SET status = 'Cancelled' WHERE id = @AppointmentId AND customer_id = @CustomerId AND status = 'Confirmed' AND starts_at_utc > now()", new { CustomerId = customerId, AppointmentId = appointmentId }, cancellationToken);
    public async Task<bool> RescheduleAsync(long customerId, long appointmentId, DateTime startAt, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition("UPDATE public.appointments a SET starts_at_utc = @StartAt AT TIME ZONE 'Africa/Johannesburg', ends_at_utc = (@StartAt + (a.ends_at_utc - a.starts_at_utc)) AT TIME ZONE 'Africa/Johannesburg' WHERE a.id = @AppointmentId AND a.customer_id = @CustomerId AND a.status = 'Confirmed' AND a.starts_at_utc > now() AND NOT EXISTS (SELECT 1 FROM public.appointments other WHERE other.customer_id = a.customer_id AND other.id <> a.id AND other.status = 'Confirmed' AND other.starts_at_utc < (@StartAt + (a.ends_at_utc - a.starts_at_utc)) AT TIME ZONE 'Africa/Johannesburg' AND other.ends_at_utc > @StartAt AT TIME ZONE 'Africa/Johannesburg')", new { CustomerId = customerId, AppointmentId = appointmentId, StartAt = startAt }, cancellationToken: cancellationToken));
        return affected == 1;
    }

    private async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, commandType: CommandType.Text, cancellationToken: cancellationToken));
    }
    private async Task<bool> ExecuteBoolAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)) == 1;
    }
}
