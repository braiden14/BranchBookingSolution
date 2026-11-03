using BranchBooking.Core.Enums;

namespace BranchBooking.Application.DTOs;

public sealed record StaffDto(
    long UserId,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserStatus Status,
    IReadOnlyList<string> Roles,
    long? BranchId,
    string? BranchName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset? InvitationExpiresAtUtc);

public sealed record PagedStaffDto(
    IReadOnlyList<StaffDto> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    int TotalPages);
