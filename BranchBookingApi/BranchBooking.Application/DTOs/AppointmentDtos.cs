using BranchBooking.Core.Enums;

namespace BranchBooking.Application.DTOs;

public sealed record CustomerBranchDto(long BranchId, string Code, string Name, string City);
public sealed record CustomerServiceDto(long ServiceId, string Code, string Name, int DurationMinutes);
public sealed record AvailableSlotDto(DateTime StartAt, DateTime EndAt);
public sealed record AppointmentDto(
    long AppointmentId,
    long BranchId,
    string BranchName,
    long ServiceId,
    string ServiceName,
    DateTime StartAt,
    DateTime EndAt,
    AppointmentStatus Status);
