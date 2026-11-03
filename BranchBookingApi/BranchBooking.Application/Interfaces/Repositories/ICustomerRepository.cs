using BranchBooking.Core.Models.Customer;

namespace BranchBooking.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<IEnumerable<CustomerRecord>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
