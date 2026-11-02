using BranchBooking.Application.DTOs;
using BranchBooking.Application.Models.Schedule;

namespace BranchBooking.Application.Interfaces.Services;

public interface IOperationalScheduleService
{
    Task<long> CreateAsync(
        long branchId,
        CreateOperationalScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalScheduleDto?> GetForBranchAsync(
        long branchId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}
