namespace BranchBooking.Application.Models.Schedule;

public sealed record WeeklyOperatingHoursRecord(
    long WeeklyHoursId,
    long ScheduleId,
    short DayOfWeek,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    bool IsClosed,
    int? Capacity);
