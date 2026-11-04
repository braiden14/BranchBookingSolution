namespace BranchBooking.Core.Models.Configuration;

public sealed record OfferedServiceRecord(
    long Id,
    string Code,
    string Name,
    int ServiceDurationMinutes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record OfferedServicePageRecord(
    long Id,
    string Code,
    string Name,
    int ServiceDurationMinutes,
    bool IsActive,
    DateTime CreatedAtUtc,
    long TotalCount);

public sealed record BranchServiceRecord(
    long BranchServiceId,
    long BranchId,
    long ServiceId,
    string ServiceCode,
    string ServiceName,
    int ServiceDurationMinutes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record DateOverrideRecord(
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

public sealed record CapacityOverrideRecord(
    long CapacityOverrideId,
    long BranchId,
    DateOnly OverrideDate,
    int Capacity);
