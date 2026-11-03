using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Schedule;
using BranchBooking.Core.Models.Schedule;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class OperationalScheduleRepository(
    DbConnectionFactory connectionFactory) : IOperationalScheduleRepository
{
    private const string CreateScheduleFunction = "SELECT public.create_schedule(@BranchId, @EffectiveFrom, @EffectiveTo, @IsActive)";
    private const string CreateWeeklyHoursFunction = "SELECT public.create_weekly_operating_hours(@ScheduleId, @DayOfWeek, @OpensAt, @ClosesAt, @IsClosed, @Capacity)";
    private const string GetScheduleFunction = "SELECT * FROM public.get_schedule_by_branch_id(@BranchId, @AsOfDate)";
    private const string GetWeeklyHoursFunction = "SELECT * FROM public.get_weekly_operating_hours_by_schedule_id(@ScheduleId)";

    public async Task<long> CreateAsync(
        long branchId,
        CreateOperationalScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var scheduleId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            CreateScheduleFunction,
            new
            {
                BranchId = branchId,
                request.EffectiveFrom,
                request.EffectiveTo,
                IsActive = true
            },
            transaction: transaction,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));

        foreach (var hours in request.WeeklyHours.OrderBy(hours => hours.DayOfWeek))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                CreateWeeklyHoursFunction,
                new
                {
                    ScheduleId = scheduleId,
                    hours.DayOfWeek,
                    hours.OpensAt,
                    hours.ClosesAt,
                    hours.IsClosed,
                    hours.Capacity
                },
                transaction: transaction,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        return scheduleId;
    }

    public async Task<OperationalScheduleRecord?> GetForBranchAsync(
        long branchId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var schedule = await connection.QuerySingleOrDefaultAsync<ScheduleRow>(new CommandDefinition(
            GetScheduleFunction,
            new { BranchId = branchId, AsOfDate = asOfDate },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));

        if (schedule is null)
        {
            return null;
        }

        var weeklyHours = await connection.QueryAsync<WeeklyOperatingHoursRecord>(new CommandDefinition(
            GetWeeklyHoursFunction,
            new { ScheduleId = schedule.Id },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));

        return new OperationalScheduleRecord(
            schedule.Id,
            schedule.BranchId,
            schedule.EffectiveFrom,
            schedule.EffectiveTo,
            schedule.IsActive,
            weeklyHours.AsList());
    }

    private sealed class ScheduleRow
    {
        public long Id { get; init; }
        public long BranchId { get; init; }
        public DateOnly EffectiveFrom { get; init; }
        public DateOnly? EffectiveTo { get; init; }
        public bool IsActive { get; init; }
    }
}
