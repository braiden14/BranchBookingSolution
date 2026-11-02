namespace BranchBooking.Application.Interfaces.Services;

public interface IBranchAccessService
{
    Task<bool> CanReadBranchAsync(
        long userId,
        bool isAdmin,
        bool isStaff,
        long branchId,
        CancellationToken cancellationToken = default);
}
