using BranchBooking.Application.DTOs;
using BranchBooking.Application.Models.Request;

namespace BranchBooking.Application.Interfaces.Services;

public interface IBranchConfigurationService
{
    Task<IEnumerable<OfferedServiceDto>> GetOfferedServicesAsync(CancellationToken cancellationToken = default);

    Task<long> CreateOfferedServiceAsync(CreateOfferedServiceRequest request, CancellationToken cancellationToken = default);

    Task UpdateOfferedServiceAsync(long serviceId, UpdateOfferedServiceRequest request, CancellationToken cancellationToken = default);

    Task DeleteOfferedServiceAsync(long serviceId, CancellationToken cancellationToken = default);

    Task<IEnumerable<BranchServiceDto>> GetBranchServicesAsync(
        long branchId,
        CancellationToken cancellationToken = default);

    Task<long> UpsertBranchServiceAsync(
        long branchId,
        UpsertBranchServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<DateOverrideDto>> GetDateOverridesAsync(
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

    Task<IEnumerable<CapacityOverrideDto>> GetCapacityOverridesAsync(
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
