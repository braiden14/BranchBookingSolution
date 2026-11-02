using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace BranchBooking.Api.Endpoints
{
    public static class BranchEndpoints
    {
        public static IEndpointRouteBuilder MapBranchEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/branches").WithTags("Branches");

            group.MapGet("/", async (IBranchService branchService, CancellationToken cancellationToken,
                [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20) =>
            {
                var branches = await branchService.GetAllAsync(pageNumber, pageSize, cancellationToken);
                return Results.Ok(branches);
            });

            group.MapPost("/", async (CreateBranchRequest request, IBranchService branchService, CancellationToken cancellationToken) =>
            {
                var branchId = await branchService.AddAsync(request, cancellationToken);
                return Results.Created($"/api/branches/{branchId}", branchId);
            });

            group.MapPost("/{branchId:long}/activate", async (
                long branchId,
                IBranchService branchService,
                CancellationToken cancellationToken) =>
            {
                await branchService.ActivateAsync(branchId, cancellationToken);
                return Results.NoContent();
            });

            return endpoints;
        }
    }
}
