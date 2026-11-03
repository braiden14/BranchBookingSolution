using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Customer;
using Dapper;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(DbConnectionFactory connectionFactory) : ICustomerRepository
{
    private const string PageSql = "SELECT * FROM public.get_customer_page(@PageSize, @Offset)";

    public async Task<IEnumerable<CustomerRecord>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<CustomerRecord>(new CommandDefinition(
            PageSql,
            new { PageSize = pageSize, Offset = (pageNumber - 1) * pageSize },
            cancellationToken: cancellationToken));
    }
}
