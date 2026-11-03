namespace BranchBooking.Application.DTOs;

public sealed record CustomerDto(
    long CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    short Status,
    DateTime CreatedAtUtc);

public sealed record PagedCustomerDto(
    IReadOnlyList<CustomerDto> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    int TotalPages);
