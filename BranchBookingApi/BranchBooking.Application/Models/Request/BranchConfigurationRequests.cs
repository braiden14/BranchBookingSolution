namespace BranchBooking.Application.Models.Request;

public sealed record UpsertBranchServiceRequest(
    long ServiceId,
    bool IsActive);

public sealed record CreateOfferedServiceRequest(string Code, string Name, int ServiceDurationMinutes);

public sealed record UpdateOfferedServiceRequest(string Code, string Name, int ServiceDurationMinutes, bool IsActive);

public sealed record CreateDateOverrideRequest(
    long ScheduleId,
    DateOnly OverrideDate,
    string? Reason,
    bool IsClosed,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    int? Capacity);

public sealed record UpdateDateOverrideRequest(
    DateOnly OverrideDate,
    string? Reason,
    bool IsClosed,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    int? Capacity);

public sealed record UpsertCapacityOverrideRequest(
    DateOnly OverrideDate,
    int Capacity);
