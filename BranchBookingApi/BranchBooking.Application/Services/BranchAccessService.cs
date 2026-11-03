using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;

namespace BranchBooking.Application.Services;

public sealed class BranchAccessService(IAuthenticationRepository authenticationRepository) : IBranchAccessService
{
    public Task<long?> GetStaffBranchIdAsync(long userId, CancellationToken cancellationToken = default) =>
        authenticationRepository.GetStaffBranchIdAsync(userId, cancellationToken);

    public async Task<bool> CanReadBranchAsync(
        long userId,
        bool isAdmin,
        bool isStaff,
        long branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId <= 0 || userId <= 0 || (!isAdmin && !isStaff))
        {
            return false;
        }

        if (isAdmin)
        {
            return true;
        }

        return await authenticationRepository.GetStaffBranchIdAsync(userId, cancellationToken) == branchId;
    }
}
