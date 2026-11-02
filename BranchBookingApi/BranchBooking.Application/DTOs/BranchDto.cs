using BranchBooking.Core.Enums;

namespace BranchBooking.Application.DTOs
{
    public record BranchDto(
        long BranchId,
        string Code,
        string Name,
        string AddressLine1,
        string City,
        string Province,
        string PostalCode,
        BranchStatus Status,
        bool AcceptsBookings,
        DateTimeOffset CreatedAtUtc);
}
