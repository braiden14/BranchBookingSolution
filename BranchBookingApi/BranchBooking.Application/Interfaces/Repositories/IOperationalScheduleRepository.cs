using BranchBooking.Application.Models.Schedule;

namespace BranchBooking.Application.Interfaces.Repositories;

public interface IOperationalScheduleRepository
{
    Task<long> CreateAsync(
        long branchId,
        CreateOperationalScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalScheduleRecord?> GetForBranchAsync(
        long branchId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}
