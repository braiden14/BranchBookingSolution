using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.DTOs;
using BranchBooking.Application.Models.Request;
using BranchBooking.Application.Models.Notifications;
using BranchBooking.Application.Models.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BranchBooking.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/customer").WithTags("Customer appointments").RequireAuthorization("CustomerOnly");
        group.MapGet("/branches", (IAppointmentService service, CancellationToken cancellationToken) => service.GetBookableBranchesAsync(cancellationToken));
        group.MapGet("/branches/{branchId:long}/services", (long branchId, IAppointmentService service, CancellationToken cancellationToken) => service.GetBookableServicesAsync(branchId, cancellationToken));
        group.MapGet("/branches/{branchId:long}/availability", async (long branchId, long serviceId, DateOnly fromDate, DateOnly toDate, long? excludingAppointmentId, ClaimsPrincipal principal, IAppointmentService service, CancellationToken cancellationToken) =>
        {
            var customerId = GetUserId(principal);
            return customerId is null
                ? Results.Unauthorized()
                : Results.Ok(await service.GetAvailableSlotsForWeekAsync(customerId.Value, branchId, serviceId, fromDate, toDate, excludingAppointmentId, cancellationToken));
        });
        group.MapGet("/appointments", async (bool history, ClaimsPrincipal principal, IAppointmentService service, CancellationToken cancellationToken) =>
        {
            var customerId = GetUserId(principal);
            return customerId is null ? Results.Unauthorized() : Results.Ok(await service.GetCustomerAppointmentsAsync(customerId.Value, history, cancellationToken));
        });
        group.MapPost("/appointments", async (CreateAppointmentRequest request, ClaimsPrincipal principal, IAppointmentService service, IAuthenticationService authenticationService, INotificationPublisher notificationPublisher, CancellationToken cancellationToken) =>
        {
            var customerId = GetUserId(principal);
            if (customerId is null) return Results.Unauthorized();
            var appointmentId = await service.CreateAsync(customerId.Value, request, cancellationToken);
            var appointment = (await service.GetCustomerAppointmentsAsync(customerId.Value, false, CancellationToken.None))
                .FirstOrDefault(item => item.AppointmentId == appointmentId);
            var customer = await authenticationService.GetCurrentUserAsync(customerId.Value, CancellationToken.None);
            if (appointment is not null)
            {
                PublishBookingConfirmation(notificationPublisher, appointment, customer);
            }
            return Results.Created($"/api/customer/appointments/{appointmentId}", new { appointmentId });
        });
        group.MapPost("/appointments/{appointmentId:long}/cancel", async (long appointmentId, ClaimsPrincipal principal, IAppointmentService service, CancellationToken cancellationToken) =>
        {
            var customerId = GetUserId(principal);
            if (customerId is null) return Results.Unauthorized();
            await service.CancelAsync(customerId.Value, appointmentId, cancellationToken);
            return Results.NoContent();
        });
        group.MapPost("/appointments/{appointmentId:long}/reschedule", async (long appointmentId, RescheduleAppointmentRequest request, ClaimsPrincipal principal, IAppointmentService service, IAuthenticationService authenticationService, INotificationPublisher notificationPublisher, CancellationToken cancellationToken) =>
        {
            var customerId = GetUserId(principal);
            if (customerId is null) return Results.Unauthorized();
            await service.RescheduleAsync(customerId.Value, appointmentId, request, cancellationToken);

            var appointment = (await service.GetCustomerAppointmentsAsync(customerId.Value, false, CancellationToken.None))
                .FirstOrDefault(item => item.AppointmentId == appointmentId);

            var customer = await authenticationService.GetCurrentUserAsync(customerId.Value, CancellationToken.None);
            if (appointment is not null)
            {
                PublishBookingConfirmation(notificationPublisher, appointment, customer);
            }
            return Results.NoContent();
        });

        var operationalGroup = endpoints.MapGroup("/api/appointments")
            .WithTags("Operational appointments")
            .RequireAuthorization("StaffOrAdmin");
        operationalGroup.MapGet("/", async (
            bool history,
            int pageNumber,
            int pageSize,
            ClaimsPrincipal principal,
            IAppointmentService service,
            IBranchAccessService branchAccessService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            long? branchId = null;
            if (!principal.IsInRole("Admin"))
            {
                branchId = await branchAccessService.GetStaffBranchIdAsync(userId.Value, cancellationToken);
                if (branchId is null) return Results.Forbid();
            }

            if (principal.IsInRole("Staff") && history) return Results.Forbid();

            return Results.Ok(await service.GetOperationalAppointmentsAsync(
                principal.IsInRole("Admin") ? null : branchId,
                history,
                pageNumber,
                pageSize,
                cancellationToken));
        });
        return endpoints;
    }

    private static long? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(value, out var id) ? id : null;
    }

    private static void PublishBookingConfirmation(INotificationPublisher publisher, AppointmentDto appointment, AuthUserDto customer)
    {
        _ = Task.Run(async () =>
        {
            var message = new NotificationMessage(
                Guid.NewGuid().ToString("N"),
                NotificationEventType.BookingConfirmation,
                customer.Email,
                $"{customer.FirstName} {customer.LastName}",
                new Dictionary<string, string>
                {
                    ["bookingReference"] = appointment.AppointmentId.ToString(),
                    ["serviceName"] = appointment.ServiceName,
                    ["startTime"] = appointment.StartAt.ToString("O")
                });
            await publisher.PublishAsync(message, CancellationToken.None);
        });
    }
}
