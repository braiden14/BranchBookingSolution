namespace BranchBooking.Application.Models.Request;

public sealed record UpdateBranchRequest(
    string Code,
    string Name,
    string AddressLine1,
    string City,
    string Province,
    string PostalCode);
