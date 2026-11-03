using BranchBooking.Core.Models.Schedule;

namespace BranchBooking.Application.DTOs;

public sealed record OperationalScheduleDto(
    long Id,
    long BranchId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    IReadOnlyList<WeeklyOperatingHoursRecord> WeeklyHours);
