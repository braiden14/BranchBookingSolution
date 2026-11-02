namespace BranchBooking.Application.Models.Schedule;

public sealed record OperationalScheduleRecord(
    long Id,
    long BranchId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    IReadOnlyList<WeeklyOperatingHoursRecord> WeeklyHours);
