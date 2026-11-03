using BranchBooking.Core.Enums;

namespace BranchBooking.Application.Models.Appointment;

public sealed record AppointmentRecord(
    long AppointmentId,
    long CustomerId,
    long BranchId,
    string BranchName,
    long ServiceId,
    string ServiceName,
    DateTime StartAt,
    DateTime EndAt,
    AppointmentStatus Status);

public sealed record BookableBranchRecord(long BranchId, string Code, string Name, string City);
public sealed record BookableServiceRecord(long ServiceId, string Code, string Name, int DurationMinutes);
public sealed record AvailableSlotRecord(DateTime StartAt, DateTime EndAt);
public sealed record OperationalAppointmentRecord(
    long AppointmentId,
    long CustomerId,
    string CustomerName,
    string CustomerEmail,
    long BranchId,
    string BranchName,
    long ServiceId,
    string ServiceName,
    DateTime StartAt,
    DateTime EndAt,
    AppointmentStatus Status,
    long TotalCount);
