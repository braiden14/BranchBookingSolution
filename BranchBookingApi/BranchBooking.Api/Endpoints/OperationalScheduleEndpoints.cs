using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Schedule;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BranchBooking.Api.Endpoints;

public static class OperationalScheduleEndpoints
{
    public static IEndpointRouteBuilder MapOperationalScheduleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/branches/{branchId:long}/operational-schedules")
            .WithTags("Branch operational schedules")
            .RequireAuthorization("StaffOrAdmin");

        group.MapPost("/", async (
            [FromRoute] long branchId,
            CreateOperationalScheduleRequest request,
            IOperationalScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var scheduleId = await scheduleService.CreateAsync(branchId, request, cancellationToken);
            return Results.Created($"/api/branches/{branchId}/operational-schedules/{scheduleId}", new { scheduleId });
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/", async (
            [FromRoute] long branchId,
            IOperationalScheduleService scheduleService,
            IBranchAccessService branchAccessService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? asOfDate = null) =>
        {
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            if (!long.TryParse(subject, out var userId) || !await branchAccessService.CanReadBranchAsync(
                    userId,
                    principal.IsInRole("Admin"),
                    principal.IsInRole("Staff"),
                    branchId,
                    cancellationToken)) return Results.Forbid();

            var schedule = await scheduleService.GetForBranchAsync(
                branchId,
                asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                cancellationToken);

            return schedule is null ? Results.NotFound() : Results.Ok(schedule);
        });

        return endpoints;
    }
}
