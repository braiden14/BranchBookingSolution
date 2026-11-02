using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Core.Entities;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories
{
    public class BranchRepository(DbConnectionFactory connectionFactory) : IBranchRepository
    {
        private const string GetAllFunction = "SELECT * FROM public.get_all_branches(@PageNumber, @PageSize)";
        private const string CreateFunction = "SELECT * FROM public.create_branch(@Code, @Name, @AddressLine1, @City, @Province, @PostalCode, @Status, @AcceptsBookings)";

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
    }
}
