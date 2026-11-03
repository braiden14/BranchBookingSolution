namespace BranchBooking.Core.Models.Branch;

public sealed record BranchListItemRecord(
    long Id,
    string Code,
    string Name,
    string AddressLine1,
    string City,
    string Province,
    string PostalCode,
    short Status,
    bool AcceptsBookings,
    DateTimeOffset CreatedAtUtc,
    long TotalCount);
