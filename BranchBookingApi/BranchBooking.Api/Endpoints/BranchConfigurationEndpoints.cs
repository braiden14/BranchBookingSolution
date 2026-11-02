using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BranchBooking.Api.Endpoints;

public static class BranchConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapBranchConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var servicesGroup = endpoints.MapGroup("/api/branches/{branchId:long}/services")
            .WithTags("Branch services")
            .RequireAuthorization("StaffOrAdmin");

        servicesGroup.MapGet("/", async (
            [FromRoute] long branchId,
            IBranchConfigurationService configurationService,
            IBranchAccessService branchAccessService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!await CanReadBranchAsync(principal, branchId, branchAccessService, cancellationToken)) return Results.Forbid();
            var services = await configurationService.GetBranchServicesAsync(branchId, cancellationToken);
            return Results.Ok(services);
        });

        servicesGroup.MapPost("/", async (
            [FromRoute] long branchId,
            UpsertBranchServiceRequest request,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var branchServiceId = await configurationService.UpsertBranchServiceAsync(
                branchId,
                request,
            cancellationToken);

            return Results.Ok(new { branchServiceId });
        }).RequireAuthorization("AdminOnly");

        endpoints.MapGet("/api/offered-services", async (
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var services = await configurationService.GetOfferedServicesAsync(cancellationToken);
            return Results.Ok(services);
        }).WithTags("Offered services").RequireAuthorization("StaffOrAdmin");

        var dateOverridesGroup = endpoints.MapGroup("/api/branches/{branchId:long}/date-overrides")
            .WithTags("Branch date overrides")
            .RequireAuthorization("StaffOrAdmin");

        dateOverridesGroup.MapGet("/", async (
            [FromRoute] long branchId,
            IBranchConfigurationService configurationService,
            IBranchAccessService branchAccessService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null) =>
        {
            if (!await CanReadBranchAsync(principal, branchId, branchAccessService, cancellationToken)) return Results.Forbid();
            var overrides = await configurationService.GetDateOverridesAsync(
                branchId,
                fromDate,
                toDate,
                cancellationToken);

            return Results.Ok(overrides);
        });

        dateOverridesGroup.MapPost("/", async (
            [FromRoute] long branchId,
            CreateDateOverrideRequest request,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var overrideId = await configurationService.CreateDateOverrideAsync(
                branchId,
                request,
                cancellationToken);

            return Results.Created($"/api/branches/{branchId}/date-overrides/{overrideId}", new { overrideId });
        }).RequireAuthorization("AdminOnly");

        dateOverridesGroup.MapPut("/{overrideId:long}", async (
            [FromRoute] long branchId,
            [FromRoute] long overrideId,
            UpdateDateOverrideRequest request,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var updated = await configurationService.UpdateDateOverrideAsync(
                branchId,
                overrideId,
                request,
                cancellationToken);

            return updated ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AdminOnly");

        dateOverridesGroup.MapDelete("/{overrideId:long}", async (
            [FromRoute] long branchId,
            [FromRoute] long overrideId,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var deleted = await configurationService.DeleteDateOverrideAsync(
                branchId,
                overrideId,
                cancellationToken);

            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AdminOnly");

        var capacityOverridesGroup = endpoints.MapGroup("/api/branches/{branchId:long}/capacity-overrides")
            .WithTags("Branch capacity overrides")
            .RequireAuthorization("StaffOrAdmin");

        capacityOverridesGroup.MapGet("/", async (
            [FromRoute] long branchId,
            IBranchConfigurationService configurationService,
            IBranchAccessService branchAccessService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null) =>
        {
            if (!await CanReadBranchAsync(principal, branchId, branchAccessService, cancellationToken)) return Results.Forbid();
            var overrides = await configurationService.GetCapacityOverridesAsync(
                branchId,
                fromDate,
                toDate,
                cancellationToken);

            return Results.Ok(overrides);
        });

        capacityOverridesGroup.MapPost("/", async (
            [FromRoute] long branchId,
            UpsertCapacityOverrideRequest request,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var overrideId = await configurationService.UpsertCapacityOverrideAsync(
                branchId,
                request,
                cancellationToken);

            return Results.Ok(new { overrideId });
        }).RequireAuthorization("AdminOnly");

        capacityOverridesGroup.MapDelete("/{capacityOverrideId:long}", async (
            [FromRoute] long branchId,
            [FromRoute] long capacityOverrideId,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var deleted = await configurationService.DeleteCapacityOverrideAsync(
                branchId,
                capacityOverrideId,
                cancellationToken);

            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AdminOnly");

        return endpoints;
    }

    private static async Task<bool> CanReadBranchAsync(
        ClaimsPrincipal principal,
        long branchId,
        IBranchAccessService branchAccessService,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return long.TryParse(subject, out var userId) &&
            await branchAccessService.CanReadBranchAsync(
                userId,
                principal.IsInRole("Admin"),
                principal.IsInRole("Staff"),
                branchId,
                cancellationToken);
    }
}
