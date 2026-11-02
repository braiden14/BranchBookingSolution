using BranchBooking.Core.Entities;

namespace BranchBooking.Application.Interfaces.Repositories
{
    public interface IBranchRepository
    {
        public Task<IEnumerable<Branch>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        public Task<long> AddAsync(Branch branch, CancellationToken cancellationToken = default);
        public Task<bool> ActivateAsync(long branchId, CancellationToken cancellationToken = default);
    }
}
