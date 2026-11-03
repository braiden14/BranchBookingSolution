using BranchBooking.Core.Models.Branch;
using BranchBooking.Application.Models.Request;

namespace BranchBooking.Application.Interfaces.Repositories
{
    public interface IBranchRepository
    {
        public Task<IEnumerable<Branch>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        public Task<long> AddAsync(Branch branch, CancellationToken cancellationToken = default);
        public Task<bool> ActivateAsync(long branchId, CancellationToken cancellationToken = default);
        public Task<IEnumerable<BranchListItemRecord>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        public Task<BranchListItemRecord?> GetByIdAsync(long branchId, CancellationToken cancellationToken = default);
        public Task<bool> UpdateAsync(long branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default);
        public Task<bool> DeactivateAsync(long branchId, CancellationToken cancellationToken = default);
    }
}
