using BranchBooking.Application.DTOs;
using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Core.Models.Branch;
using BranchBooking.Application.Models.Request;
using BranchBooking.Core.Models.Schedule;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;

namespace BranchBooking.Application.Services
{
    public class BranchService(
        IBranchRepository branchRepository,
        IOperationalScheduleRepository? scheduleRepository = null,
        IBranchConfigurationRepository? configurationRepository = null) : IBranchService
    {
        public async Task<IEnumerable<BranchDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var branches = await branchRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
            return branches.Select(MapToDto).ToArray();
        }

        public async Task<long> AddAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var branch = new Branch(
                request.Code,
                request.Name.Trim(),
                request.AddressLine1.Trim(),
                request.City.Trim(),
                request.Province.Trim(),
                request.PostalCode.Trim(),
                BranchStatus.Inactive, false);

            return await branchRepository.AddAsync(branch, cancellationToken);
        }

        public async Task<PagedBranchDto> GetPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var records = (await branchRepository.GetPageAsync(pageNumber, pageSize, cancellationToken)).ToArray();
            var totalCount = records.FirstOrDefault()?.TotalCount ?? 0;
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedBranchDto(
                records.Select(MapToDto).ToArray(),
                pageNumber,
                pageSize,
                totalCount,
                totalPages);
        }

        public async Task<BranchDto?> GetByIdAsync(long branchId, CancellationToken cancellationToken = default)
        {
            ValidateBranchId(branchId);
            var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
            return branch is null ? null : MapToDto(branch);
        }

        public async Task UpdateAsync(
            long branchId,
            UpdateBranchRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateBranchId(branchId);
            ValidateUpdateRequest(request);

            if (!await branchRepository.UpdateAsync(branchId, request, cancellationToken))
            {
                throw new DomainValidationException("The branch could not be found.");
            }
        }

        public async Task DeactivateAsync(long branchId, CancellationToken cancellationToken = default)
        {
            ValidateBranchId(branchId);

            if (!await branchRepository.DeactivateAsync(branchId, cancellationToken))
            {
                throw new DomainValidationException("The branch could not be found.");
            }
        }

        public async Task ActivateAsync(long branchId, CancellationToken cancellationToken = default)
        {
            if (branchId <= 0)
            {
                throw new DomainValidationException("Branch ID must be greater than zero.");
            }

            if (scheduleRepository is null || configurationRepository is null)
            {
                throw new DomainValidationException("Branch activation dependencies are not configured.");
            }

            var schedule = await scheduleRepository.GetForBranchAsync(
                branchId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                cancellationToken);

            if (schedule is null || !HasValidOperatingHours(schedule.WeeklyHours))
            {
                throw new DomainValidationException(
                    "The branch must have valid operational hours before it can be activated.");
            }

            var branchServices = await configurationRepository.GetBranchServicesAsync(
                branchId,
                cancellationToken);

            if (!branchServices.Any(service => service.IsActive))
            {
                throw new DomainValidationException(
                    "The branch must have at least one active service before it can be activated.");
            }

            if (!await branchRepository.ActivateAsync(branchId, cancellationToken))
            {
                throw new DomainValidationException("The branch could not be found.");
            }
        }

        private void ValidateRequest(CreateBranchRequest? request)
        {
            if (request is null)
            {
                throw new DomainValidationException("Branch request is required.");
            }
        }

        private static bool HasValidOperatingHours(IEnumerable<WeeklyOperatingHoursRecord> weeklyHours)
        {
            var hours = weeklyHours.ToArray();
            return hours.Length == 7 &&
                hours.Select(day => day.DayOfWeek).Distinct().Count() == 7 &&
                hours.All(day => day.IsClosed ||
                    (day.OpensAt.HasValue &&
                     day.ClosesAt.HasValue &&
                     day.ClosesAt > day.OpensAt &&
                     day.Capacity is > 0));
        }

        private static BranchDto MapToDto(Branch branch)
        {
            return new BranchDto(
                branch.Id,
                branch.Code,
                branch.Name,
                branch.AddressLine1,
                branch.City,
                branch.Province,
                branch.PostalCode,
                branch.Status,
                branch.AcceptsBookings,
                branch.CreatedAtUtc);
        }

        private static BranchDto MapToDto(BranchListItemRecord branch)
        {
            return new BranchDto(
                branch.Id,
                branch.Code,
                branch.Name,
                branch.AddressLine1,
                branch.City,
                branch.Province,
                branch.PostalCode,
                (BranchStatus)branch.Status,
                branch.AcceptsBookings,
                branch.CreatedAtUtc);
        }

        private static void ValidateUpdateRequest(UpdateBranchRequest? request)
        {
            if (request is null)
            {
                throw new DomainValidationException("Branch update request is required.");
            }

            ValidateText(request.Code, "Code", 50);
            ValidateText(request.Name, "Name", 200);
            ValidateText(request.AddressLine1, "AddressLine1", 200);
            ValidateText(request.City, "City", 100);
            ValidateText(request.Province, "Province", 100);
            ValidateText(request.PostalCode, "PostalCode", 20);
        }

        private static void ValidateText(string? value, string fieldName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainValidationException($"{fieldName} is required.");
            }

            if (value.Length > maxLength)
            {
                throw new DomainValidationException($"{fieldName} cannot exceed {maxLength} characters.");
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
}
