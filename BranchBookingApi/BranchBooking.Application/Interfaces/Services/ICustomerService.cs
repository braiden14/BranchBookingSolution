using BranchBooking.Application.DTOs;

namespace BranchBooking.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<PagedCustomerDto> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
