using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace BranchBooking.Api.Endpoints;

public static class BranchConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapBranchConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var servicesGroup = endpoints.MapGroup("/api/branches/{branchId:long}/services")
            .WithTags("Branch services");

        servicesGroup.MapGet("/", async (
            [FromRoute] long branchId,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
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
        });

        endpoints.MapGet("/api/offered-services", async (
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken) =>
        {
            var services = await configurationService.GetOfferedServicesAsync(cancellationToken);
            return Results.Ok(services);
        }).WithTags("Offered services");

        var dateOverridesGroup = endpoints.MapGroup("/api/branches/{branchId:long}/date-overrides")
            .WithTags("Branch date overrides");

        dateOverridesGroup.MapGet("/", async (
            [FromRoute] long branchId,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null) =>
        {
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
        });

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
        });

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
        });

        var capacityOverridesGroup = endpoints.MapGroup("/api/branches/{branchId:long}/capacity-overrides")
            .WithTags("Branch capacity overrides");

        capacityOverridesGroup.MapGet("/", async (
            [FromRoute] long branchId,
            IBranchConfigurationService configurationService,
            CancellationToken cancellationToken,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null) =>
        {
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
        });

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
        });

        return endpoints;
    }
}
