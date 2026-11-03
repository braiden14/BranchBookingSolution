namespace BranchBooking.Core.Models.Schedule;

public sealed record OperationalScheduleRecord(
    long Id,
    long BranchId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    IReadOnlyList<WeeklyOperatingHoursRecord> WeeklyHours);

public sealed record WeeklyOperatingHoursRecord(
    long WeeklyHoursId,
    long ScheduleId,
    short DayOfWeek,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    bool IsClosed,
    int? Capacity);
