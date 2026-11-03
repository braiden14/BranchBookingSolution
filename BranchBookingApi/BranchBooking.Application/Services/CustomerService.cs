using BranchBooking.Application.DTOs;
using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;

namespace BranchBooking.Application.Services;

public sealed class CustomerService(ICustomerRepository customerRepository) : ICustomerService
{
    public async Task<PagedCustomerDto> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var records = (await customerRepository.GetPageAsync(pageNumber, pageSize, cancellationToken)).ToArray();
        var totalCount = records.FirstOrDefault()?.TotalCount ?? 0;
        return new PagedCustomerDto(
            records.Select(customer => new CustomerDto(
                customer.CustomerId,
                customer.FirstName,
                customer.LastName,
                customer.Email,
                customer.PhoneNumber,
                customer.Status,
                customer.CreatedAtUtc)).ToArray(),
            pageNumber,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
