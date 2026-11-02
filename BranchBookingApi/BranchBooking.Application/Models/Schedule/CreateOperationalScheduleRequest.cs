namespace BranchBooking.Application.Models.Schedule;

public sealed record CreateOperationalScheduleRequest(
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyList<WeeklyOperatingHoursRequest> WeeklyHours);
