using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Customer;
using Dapper;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(DbConnectionFactory connectionFactory) : ICustomerRepository
{
    private const string PageSql = "SELECT u.id AS CustomerId, u.first_name AS FirstName, u.last_name AS LastName, u.email AS Email, u.phone_number AS PhoneNumber, u.status AS Status, u.created_at_utc AS CreatedAtUtc, COUNT(*) OVER() AS TotalCount FROM public.users u JOIN public.customer_profiles cp ON cp.user_id = u.id ORDER BY u.last_name, u.first_name, u.id LIMIT @PageSize OFFSET @Offset";

    public async Task<IEnumerable<CustomerRecord>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<CustomerRecord>(new CommandDefinition(
            PageSql,
            new { PageSize = pageSize, Offset = (pageNumber - 1) * pageSize },
            cancellationToken: cancellationToken));
    }
}
