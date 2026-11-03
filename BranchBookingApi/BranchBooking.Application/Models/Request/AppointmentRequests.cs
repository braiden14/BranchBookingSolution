namespace BranchBooking.Application.Models.Request;

public sealed record CreateAppointmentRequest(long BranchId, long ServiceId, DateTime StartAt);
public sealed record RescheduleAppointmentRequest(DateTime StartAt);
