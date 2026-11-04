using BranchBooking.Core.Models.Configuration;

namespace BranchBooking.Application.DTOs;

public sealed record OfferedServiceDto(
    long Id,
    string Code,
    string Name,
    int ServiceDurationMinutes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record PagedOfferedServiceDto(
    IReadOnlyList<OfferedServiceDto> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    int TotalPages);

public sealed record BranchServiceDto(
    long BranchServiceId,
    long BranchId,
    long ServiceId,
    string ServiceCode,
    string ServiceName,
    int ServiceDurationMinutes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record DateOverrideDto(
    long OverrideId,
    long ScheduleId,
    long BranchId,
    DateOnly OverrideDate,
    string? Reason,
    bool IsClosed,
    long? DateHoursId,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    int? Capacity);

public sealed record CapacityOverrideDto(
    long CapacityOverrideId,
    long BranchId,
    DateOnly OverrideDate,
    int Capacity);
