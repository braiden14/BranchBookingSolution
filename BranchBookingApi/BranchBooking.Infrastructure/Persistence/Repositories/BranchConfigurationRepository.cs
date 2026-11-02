using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Configuration;
using BranchBooking.Application.Models.Request;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class BranchConfigurationRepository(DbConnectionFactory connectionFactory) : IBranchConfigurationRepository
{
    private const string GetOfferedServicesFunction = "SELECT * FROM public.get_all_offered_services()";
    private const string GetBranchServicesFunction = "SELECT * FROM public.get_branch_services(@BranchId)";
    private const string UpsertBranchServiceFunction = "SELECT public.upsert_branch_service(@BranchId, @ServiceId, @IsActive)";

    private const string GetDateOverridesFunction = "SELECT * FROM public.get_branch_date_overrides(@BranchId, @FromDate, @ToDate)";
    private const string CreateDateOverrideFunction = "SELECT public.create_branch_date_override(@ScheduleId, @OverrideDate, @Reason, @IsClosed)";
    private const string UpdateDateOverrideFunction = "SELECT public.update_branch_date_override(@OverrideId, @OverrideDate, @Reason, @IsClosed)";
    private const string CreateDateOperatingHoursFunction = "SELECT public.create_branch_date_operating_hours(@OverrideId, @OpensAt, @ClosesAt, @Capacity)";
    private const string UpdateDateOperatingHoursFunction = "SELECT public.update_branch_date_operating_hours(@DateHoursId, @OpensAt, @ClosesAt, @Capacity)";
    private const string DeleteDateOverrideFunction = "SELECT public.delete_branch_date_override(@OverrideId)";

    private const string GetCapacityOverridesFunction = "SELECT * FROM public.get_branch_capacity_overrides(@BranchId, @FromDate, @ToDate)";
    private const string UpsertCapacityOverrideFunction = "SELECT public.upsert_branch_daily_capacity_override(@BranchId, @OverrideDate, @Capacity)";
    private const string DeleteCapacityOverrideFunction = "SELECT public.delete_branch_daily_capacity_override(@CapacityOverrideId)";

    public async Task<IEnumerable<OfferedServiceRecord>> GetOfferedServicesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<OfferedServiceRecord>(new CommandDefinition(
            GetOfferedServicesFunction,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<BranchServiceRecord>> GetBranchServicesAsync(
        long branchId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<BranchServiceRecord>(new CommandDefinition(
            GetBranchServicesFunction,
            new { BranchId = branchId },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }

    public async Task<long> UpsertBranchServiceAsync(
        long branchId,
        UpsertBranchServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            UpsertBranchServiceFunction,
            new
            {
                BranchId = branchId,
                request.ServiceId,
                request.IsActive
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<DateOverrideRecord>> GetDateOverridesAsync(
        long branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<DateOverrideRecord>(new CommandDefinition(
            GetDateOverridesFunction,
            new
            {
                BranchId = branchId,
                FromDate = fromDate,
                ToDate = toDate
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }

    public async Task<long> CreateDateOverrideAsync(
        long branchId,
        CreateDateOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var overrideId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            CreateDateOverrideFunction,
            new
            {
                request.ScheduleId,
                request.OverrideDate,
                request.Reason,
                request.IsClosed
            },
            transaction: transaction,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));

        if (!request.IsClosed)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                CreateDateOperatingHoursFunction,
                new
                {
                    OverrideId = overrideId,
                    request.OpensAt,
                    request.ClosesAt,
                    request.Capacity
                },
                transaction: transaction,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        return overrideId;
    }

    public async Task<bool> UpdateDateOverrideAsync(
        long branchId,
        long overrideId,
        UpdateDateOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var currentOverride = (await connection.QueryAsync<DateOverrideRecord>(new CommandDefinition(
            GetDateOverridesFunction,
            new
            {
                BranchId = branchId,
                FromDate = (DateOnly?)null,
                ToDate = (DateOnly?)null
            },
            transaction: transaction,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken)))
            .SingleOrDefault(dateOverride => dateOverride.OverrideId == overrideId);

        if (currentOverride is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var updated = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            UpdateDateOverrideFunction,
            new
            {
                OverrideId = overrideId,
                request.OverrideDate,
                request.Reason,
                request.IsClosed
            },
            transaction: transaction,
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));

        if (updated && !request.IsClosed)
        {
            if (currentOverride.DateHoursId.HasValue)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    UpdateDateOperatingHoursFunction,
                    new
                    {
                        DateHoursId = currentOverride.DateHoursId.Value,
                        request.OpensAt,
                        request.ClosesAt,
                        request.Capacity
                    },
                    transaction: transaction,
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));
            }
            else
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    CreateDateOperatingHoursFunction,
                    new
                    {
                        OverrideId = overrideId,
                        request.OpensAt,
                        request.ClosesAt,
                        request.Capacity
                    },
                    transaction: transaction,
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    public async Task<bool> DeleteDateOverrideAsync(
        long branchId,
        long overrideId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            DeleteDateOverrideFunction,
            new { OverrideId = overrideId },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<CapacityOverrideRecord>> GetCapacityOverridesAsync(
        long branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<CapacityOverrideRecord>(new CommandDefinition(
            GetCapacityOverridesFunction,
            new
            {
                BranchId = branchId,
                FromDate = fromDate,
                ToDate = toDate
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }

    public async Task<long> UpsertCapacityOverrideAsync(
        long branchId,
        UpsertCapacityOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            UpsertCapacityOverrideFunction,
            new
            {
                BranchId = branchId,
                request.OverrideDate,
                request.Capacity
            },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteCapacityOverrideAsync(
        long branchId,
        long capacityOverrideId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            DeleteCapacityOverrideFunction,
            new { CapacityOverrideId = capacityOverrideId },
            commandType: CommandType.Text,
            cancellationToken: cancellationToken));
    }
}
