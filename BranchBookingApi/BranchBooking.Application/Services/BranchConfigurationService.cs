using BranchBooking.Application.DTOs;
using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Core.Models.Configuration;
using BranchBooking.Application.Models.Request;
using BranchBooking.Core.Exceptions;

namespace BranchBooking.Application.Services;

public sealed class BranchConfigurationService(
    IBranchConfigurationRepository configurationRepository,
    IOperationalScheduleRepository scheduleRepository) : IBranchConfigurationService
{
    public async Task<IEnumerable<OfferedServiceDto>> GetOfferedServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = await configurationRepository.GetOfferedServicesAsync(cancellationToken);
        return services.Select(service => new OfferedServiceDto(
            service.Id,
            service.Code,
            service.Name,
            service.ServiceDurationMinutes,
            service.IsActive,
            service.CreatedAtUtc)).ToArray();
    }

    public async Task<PagedOfferedServiceDto> GetOfferedServicesPageAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        var records = (await configurationRepository.GetOfferedServicesPageAsync(
            pageNumber,
            pageSize,
            searchTerm,
            isActive,
            cancellationToken)).ToArray();
        var totalCount = records.FirstOrDefault()?.TotalCount ?? await configurationRepository.CountOfferedServicesAsync(searchTerm, isActive, cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedOfferedServiceDto(
            records.Select(service => new OfferedServiceDto(
                service.Id,
                service.Code,
                service.Name,
                service.ServiceDurationMinutes,
                service.IsActive,
                service.CreatedAtUtc)).ToArray(),
            pageNumber,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<long> CreateOfferedServiceAsync(CreateOfferedServiceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateOfferedService(request.Code, request.Name, request.ServiceDurationMinutes);
        return await configurationRepository.CreateOfferedServiceAsync(request with { Code = request.Code.Trim(), Name = request.Name.Trim() }, cancellationToken);
    }

    public async Task UpdateOfferedServiceAsync(long serviceId, UpdateOfferedServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0) throw new DomainValidationException("A valid service is required.");
        ValidateOfferedService(request.Code, request.Name, request.ServiceDurationMinutes);
        await configurationRepository.UpdateOfferedServiceAsync(serviceId, request with { Code = request.Code.Trim(), Name = request.Name.Trim() }, cancellationToken);
    }

    public async Task DeleteOfferedServiceAsync(long serviceId, CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0) throw new DomainValidationException("A valid service is required.");
        if (!await configurationRepository.DeleteOfferedServiceAsync(serviceId, cancellationToken))
        {
            throw new DomainValidationException("The offered service could not be found.");
        }
    }

    public async Task<IEnumerable<BranchServiceDto>> GetBranchServicesAsync(
        long branchId,
        CancellationToken cancellationToken = default)
    {
        ValidateBranchId(branchId);
        var services = await configurationRepository.GetBranchServicesAsync(branchId, cancellationToken);
        return services.Select(service => new BranchServiceDto(
            service.BranchServiceId,
            service.BranchId,
            service.ServiceId,
            service.ServiceCode,
            service.ServiceName,
            service.ServiceDurationMinutes,
            service.IsActive,
            service.CreatedAtUtc)).ToArray();
    }

    private static void ValidateOfferedService(string code, string name, int duration)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || code.Trim().Length > 50 || name.Trim().Length > 200 || duration <= 0)
            throw new DomainValidationException("Service code, name, and a positive duration are required.");
    }

    public async Task<long> UpsertBranchServiceAsync(
        long branchId,
        UpsertBranchServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateBranchId(branchId);
        if (request is null || request.ServiceId <= 0)
        {
            throw new DomainValidationException("A valid service is required.");
        }

        return await configurationRepository.UpsertBranchServiceAsync(branchId, request, cancellationToken);
    }

    public async Task<IEnumerable<DateOverrideDto>> GetDateOverridesAsync(
        long branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(branchId, fromDate, toDate);
        var overrides = await configurationRepository.GetDateOverridesAsync(branchId, fromDate, toDate, cancellationToken);
        return overrides.Select(MapDateOverride).ToArray();
    }

    public async Task<long> CreateDateOverrideAsync(
        long branchId,
        CreateDateOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOverride(branchId, request?.OverrideDate, request?.IsClosed, request?.OpensAt, request?.ClosesAt, request?.Capacity);
        ArgumentNullException.ThrowIfNull(request);
        await EnsureScheduleBelongsToBranchAsync(branchId, request.ScheduleId, request.OverrideDate, cancellationToken);
        return await configurationRepository.CreateDateOverrideAsync(branchId, request, cancellationToken);
    }

    public async Task<bool> UpdateDateOverrideAsync(
        long branchId,
        long overrideId,
        UpdateDateOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateBranchId(branchId);
        if (overrideId <= 0)
        {
            throw new DomainValidationException("Override ID must be greater than zero.");
        }

        ValidateDateOverride(branchId, request?.OverrideDate, request?.IsClosed, request?.OpensAt, request?.ClosesAt, request?.Capacity);
        ArgumentNullException.ThrowIfNull(request);
        await EnsureDateOverrideBelongsToBranchAsync(branchId, overrideId, cancellationToken);
        return await configurationRepository.UpdateDateOverrideAsync(branchId, overrideId, request, cancellationToken);
    }

    public async Task<bool> DeleteDateOverrideAsync(
        long branchId,
        long overrideId,
        CancellationToken cancellationToken = default)
    {
        ValidateBranchId(branchId);
        if (overrideId <= 0)
        {
            throw new DomainValidationException("Override ID must be greater than zero.");
        }

        await EnsureDateOverrideBelongsToBranchAsync(branchId, overrideId, cancellationToken);
        return await configurationRepository.DeleteDateOverrideAsync(branchId, overrideId, cancellationToken);
    }

    public async Task<IEnumerable<CapacityOverrideDto>> GetCapacityOverridesAsync(
        long branchId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(branchId, fromDate, toDate);
        var overrides = await configurationRepository.GetCapacityOverridesAsync(branchId, fromDate, toDate, cancellationToken);
        return overrides.Select(overrideRecord => new CapacityOverrideDto(
            overrideRecord.CapacityOverrideId,
            overrideRecord.BranchId,
            overrideRecord.OverrideDate,
            overrideRecord.Capacity)).ToArray();
    }

    public async Task<long> UpsertCapacityOverrideAsync(
        long branchId,
        UpsertCapacityOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateBranchId(branchId);
        if (request is null)
        {
            throw new DomainValidationException("Capacity override request is required.");
        }

        if (request.Capacity < 0)
        {
            throw new DomainValidationException("Capacity cannot be negative.");
        }

        return await configurationRepository.UpsertCapacityOverrideAsync(branchId, request, cancellationToken);
    }

    public async Task<bool> DeleteCapacityOverrideAsync(
        long branchId,
        long capacityOverrideId,
        CancellationToken cancellationToken = default)
    {
        ValidateBranchId(branchId);
        if (capacityOverrideId <= 0)
        {
            throw new DomainValidationException("Capacity override ID must be greater than zero.");
        }

        var overrides = await configurationRepository.GetCapacityOverridesAsync(branchId, null, null, cancellationToken);
        if (!overrides.Any(overrideRecord => overrideRecord.CapacityOverrideId == capacityOverrideId))
        {
            throw new DomainValidationException("Capacity override was not found for this branch.");
        }

        return await configurationRepository.DeleteCapacityOverrideAsync(branchId, capacityOverrideId, cancellationToken);
    }

    private async Task EnsureScheduleBelongsToBranchAsync(
        long branchId,
        long scheduleId,
        DateOnly overrideDate,
        CancellationToken cancellationToken)
    {
        if (scheduleId <= 0)
        {
            throw new DomainValidationException("A valid operational schedule is required.");
        }

        //var schedule = await scheduleRepository.GetForBranchAsync(branchId, overrideDate, cancellationToken);
        //if (schedule is null || schedule.Id != scheduleId)
        //{
        //    throw new DomainValidationException("The operational schedule does not belong to this branch or date.");
        //}
    }

    private async Task EnsureDateOverrideBelongsToBranchAsync(
        long branchId,
        long overrideId,
        CancellationToken cancellationToken)
    {
        var overrides = await configurationRepository.GetDateOverridesAsync(branchId, null, null, cancellationToken);
        if (!overrides.Any(overrideRecord => overrideRecord.OverrideId == overrideId))
        {
            throw new DomainValidationException("Date override was not found for this branch.");
        }
    }

    private static DateOverrideDto MapDateOverride(DateOverrideRecord dateOverride) => new(
        dateOverride.OverrideId,
        dateOverride.ScheduleId,
        dateOverride.BranchId,
        dateOverride.OverrideDate,
        dateOverride.Reason,
        dateOverride.IsClosed,
        dateOverride.DateHoursId,
        dateOverride.OpensAt,
        dateOverride.ClosesAt,
        dateOverride.Capacity);

    private static void ValidateDateOverride(
        long branchId,
        DateOnly? overrideDate,
        bool? isClosed,
        TimeOnly? opensAt,
        TimeOnly? closesAt,
        int? capacity)
    {
        ValidateBranchId(branchId);
        if (!overrideDate.HasValue)
        {
            throw new DomainValidationException("Override date is required.");
        }

        if (capacity is < 0)
        {
            throw new DomainValidationException("Capacity cannot be negative.");
        }

        if (isClosed == true)
        {
            if (opensAt.HasValue || closesAt.HasValue || capacity.HasValue)
            {
                throw new DomainValidationException("A closed date cannot have operating hours or capacity.");
            }

            return;
        }

        if (!opensAt.HasValue || !closesAt.HasValue || closesAt <= opensAt)
        {
            throw new DomainValidationException("An open date must have valid opening and closing times.");
        }
    }

    private static void ValidateDateRange(long branchId, DateOnly? fromDate, DateOnly? toDate)
    {
        ValidateBranchId(branchId);
        if (fromDate.HasValue && toDate.HasValue && toDate < fromDate)
        {
            throw new DomainValidationException("The end date cannot be before the start date.");
        }
    }

    private static void ValidateBranchId(long branchId)
    {
        if (branchId <= 0)
        {
            throw new DomainValidationException("Branch ID must be greater than zero.");
        }
    }
}
