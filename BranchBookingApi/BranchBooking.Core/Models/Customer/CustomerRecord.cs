namespace BranchBooking.Core.Models.Customer;

public sealed record CustomerRecord(
    long CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    short Status,
    DateTime CreatedAtUtc,
    long TotalCount);
