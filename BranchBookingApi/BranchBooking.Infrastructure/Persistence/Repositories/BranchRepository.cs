using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Core.Models.Branch;
using BranchBooking.Application.Models.Request;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories
{
    public class BranchRepository(DbConnectionFactory connectionFactory) : IBranchRepository
    {
        private const string GetAllFunction = "SELECT * FROM public.get_all_branches(@PageNumber, @PageSize)";
        private const string CreateFunction = "SELECT * FROM public.create_branch(@Code, @Name, @AddressLine1, @City, @Province, @PostalCode, @Status, @AcceptsBookings)";
        private const string ActivateFunction = "SELECT public.activate_branch(@BranchId)";
        private const string UpdateFunction = "SELECT public.update_branch(@BranchId, @Code, @Name, @AddressLine1, @City, @Province, @PostalCode)";
        private const string DeactivateFunction = "SELECT public.deactivate_branch(@BranchId)";
        private const string GetByIdFunction = "SELECT * FROM public.get_branch_by_id(@BranchId)";

        public async Task<long> AddAsync(Branch branch, CancellationToken cancellationToken = default)
        {
            await using var connection = connectionFactory.CreateConnection();

            var id = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    CreateFunction,
                    new
                    {
                        branch.Code,
                        branch.Name,
                        branch.AddressLine1,
                        branch.City,
                        branch.Province,
                        branch.PostalCode,
                        Status = (short)branch.Status,
                        branch.AcceptsBookings
                    },
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));
                        
            return id;
        }

        public async Task<IEnumerable<Branch>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            await using var connection = connectionFactory.CreateConnection();

            var branches = await connection.QueryAsync<Branch>(
                new CommandDefinition(
                    GetAllFunction,
                    new
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    },
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));

            return branches.AsList();
        }

        public async Task<bool> ActivateAsync(long branchId, CancellationToken cancellationToken = default)
        {
            await using var connection = connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    ActivateFunction,
                    new { BranchId = branchId },
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<BranchListItemRecord>> GetPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            await using var connection = connectionFactory.CreateConnection();

            // The database function returns PostgreSQL column names (including
            // underscores) and timestamp without time zone maps to DateTime.
            // Materializing directly into BranchListItemRecord therefore makes
            // Dapper look for a constructor with a different signature.
            var rows = await connection.QueryAsync<BranchListItemDbRow>(
                new CommandDefinition(
                    GetAllFunction,
                    new
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    },
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));

            return rows.Select(row => new BranchListItemRecord(
                row.id,
                row.code,
                row.name,
                row.address_line_1,
                row.city,
                row.province,
                row.postal_code,
                row.status,
                row.accepts_bookings,
                new DateTimeOffset(DateTime.SpecifyKind(row.created_at_utc, DateTimeKind.Utc)),
                row.total_count)).ToArray();
        }

        public async Task<BranchListItemRecord?> GetByIdAsync(long branchId, CancellationToken cancellationToken = default)
        {
            await using var connection = connectionFactory.CreateConnection();
            var row = await connection.QuerySingleOrDefaultAsync<BranchListItemDbRow>(new CommandDefinition(
                GetByIdFunction,
                new { BranchId = branchId },
                cancellationToken: cancellationToken));

            return row is null
                ? null
                : new BranchListItemRecord(
                    row.id,
                    row.code,
                    row.name,
                    row.address_line_1,
                    row.city,
                    row.province,
                    row.postal_code,
                    row.status,
                    row.accepts_bookings,
                    new DateTimeOffset(DateTime.SpecifyKind(row.created_at_utc, DateTimeKind.Utc)),
                    row.total_count);
        }

        private sealed record BranchListItemDbRow(
            long id,
            string code,
            string name,
            string address_line_1,
            string city,
            string province,
            string postal_code,
            short status,
            bool accepts_bookings,
            DateTime created_at_utc,
            long total_count);

        public async Task<bool> UpdateAsync(
            long branchId,
            UpdateBranchRequest request,
            CancellationToken cancellationToken = default)
        {
            await using var connection = connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    UpdateFunction,
                    new
                    {
                        BranchId = branchId,
                        request.Code,
                        request.Name,
                        request.AddressLine1,
                        request.City,
                        request.Province,
                        request.PostalCode
                    },
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeactivateAsync(long branchId, CancellationToken cancellationToken = default)
        {
            await using var connection = connectionFactory.CreateConnection();

            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    DeactivateFunction,
                    new { BranchId = branchId },
                    commandType: CommandType.Text,
                    cancellationToken: cancellationToken));
        }
    }
}
