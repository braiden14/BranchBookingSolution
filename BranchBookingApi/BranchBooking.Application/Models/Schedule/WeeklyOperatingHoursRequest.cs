namespace BranchBooking.Application.Models.Schedule;

public sealed record WeeklyOperatingHoursRequest(
    short DayOfWeek,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    bool IsClosed,
    int? Capacity);
