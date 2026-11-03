using BranchBooking.Core.Models.Configuration;
using BranchBooking.Application.Models.Request;

namespace BranchBooking.Application.Interfaces.Repositories;

public interface IBranchConfigurationRepository
{
    Task<IEnumerable<OfferedServiceRecord>> GetOfferedServicesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<BranchServiceRecord>> GetBranchServicesAsync(
        long branchId,
        CancellationToken cancellationToken = default);

    Task<long> UpsertBranchServiceAsync(
        long branchId,
        UpsertBranchServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<DateOverrideRecord>> GetDateOverridesAsync(
        long branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<long> CreateDateOverrideAsync(
        long branchId,
        CreateDateOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateDateOverrideAsync(
        long branchId,
        long overrideId,
        UpdateDateOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDateOverrideAsync(
        long branchId,
        long overrideId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<CapacityOverrideRecord>> GetCapacityOverridesAsync(
        long branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    Task<long> UpsertCapacityOverrideAsync(
        long branchId,
        UpsertCapacityOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCapacityOverrideAsync(
        long branchId,
        long capacityOverrideId,
        CancellationToken cancellationToken = default);
}
