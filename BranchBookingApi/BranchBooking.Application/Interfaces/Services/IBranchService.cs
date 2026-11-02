using BranchBooking.Application.DTOs;
using BranchBooking.Application.Models.Request;
using BranchBooking.Core.Entities;

namespace BranchBooking.Application.Interfaces.Services
{
    public interface IBranchService
    {
        public Task<IEnumerable<BranchDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<long> AddAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);
    }
}
