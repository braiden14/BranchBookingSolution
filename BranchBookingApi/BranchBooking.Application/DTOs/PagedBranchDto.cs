namespace BranchBooking.Application.DTOs;

public sealed record PagedBranchDto(
    IReadOnlyList<BranchDto> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    int TotalPages);
