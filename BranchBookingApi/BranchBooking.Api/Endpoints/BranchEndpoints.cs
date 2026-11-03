using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Request;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BranchBooking.Api.Endpoints
{
    public static class BranchEndpoints
    {
        public static IEndpointRouteBuilder MapBranchEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/branches").WithTags("Branches");

            group.MapGet("/", async (IBranchService branchService, CancellationToken cancellationToken,
                [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5, [FromQuery] string? searchTerm = null) =>
            {
                var branches = await branchService.GetPageAsync(pageNumber, pageSize, cancellationToken, searchTerm);
                return Results.Ok(branches);
            }).RequireAuthorization("AdminOnly");

            group.MapGet("/{branchId:long}", async (
                long branchId,
                IBranchService branchService,
                IBranchAccessService branchAccessService,
                ClaimsPrincipal principal,
                CancellationToken cancellationToken) =>
            {
                var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
                if (!long.TryParse(subject, out var userId) || !await branchAccessService.CanReadBranchAsync(
                        userId,
                        principal.IsInRole("Admin"),
                        principal.IsInRole("Staff") || principal.IsInRole("BranchManager"),
                        branchId,
                        cancellationToken)) return Results.Forbid();

                var branch = await branchService.GetByIdAsync(branchId, cancellationToken);
                return branch is null ? Results.NotFound() : Results.Ok(branch);
            }).RequireAuthorization("StaffOrAdmin");

            group.MapPost("/", async (CreateBranchRequest request, IBranchService branchService, CancellationToken cancellationToken) =>
            {
                var branchId = await branchService.AddAsync(request, cancellationToken);
                return Results.Created($"/api/branches/{branchId}", branchId);
            }).RequireAuthorization("AdminOnly");

            group.MapPost("/{branchId:long}/activate", async (
                long branchId,
                IBranchService branchService,
                CancellationToken cancellationToken) =>
            {
                await branchService.ActivateAsync(branchId, cancellationToken);
                return Results.NoContent();
            }).RequireAuthorization("AdminOnly");

            group.MapPut("/{branchId:long}", async (
                long branchId,
                UpdateBranchRequest request,
                IBranchService branchService,
                CancellationToken cancellationToken) =>
            {
                await branchService.UpdateAsync(branchId, request, cancellationToken);
                return Results.NoContent();
            }).RequireAuthorization("AdminOnly");

            group.MapPost("/{branchId:long}/deactivate", async (
                long branchId,
                IBranchService branchService,
                CancellationToken cancellationToken) =>
            {
                await branchService.DeactivateAsync(branchId, cancellationToken);
                return Results.NoContent();
            }).RequireAuthorization("AdminOnly");

            return endpoints;
        }
    }
}
