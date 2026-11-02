using BranchBooking.Application.DTOs;
using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Request;
using BranchBooking.Core.Entities;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;

namespace BranchBooking.Application.Services
{
    public class BranchService(IBranchRepository branchRepository) : IBranchService
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

        private void ValidateRequest(CreateBranchRequest? request)
        {
            if (request is null)
            {
                throw new DomainValidationException("Branch request is required.");
            }
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
    }
}
