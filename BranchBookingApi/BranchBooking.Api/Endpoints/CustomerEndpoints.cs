using BranchBooking.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BranchBooking.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/customers", async (
            ICustomerService customerService,
            CancellationToken cancellationToken,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20) =>
        {
            return Results.Ok(await customerService.GetPageAsync(pageNumber, pageSize, cancellationToken));
        })
        .WithTags("Customers")
        .RequireAuthorization("AdminOnly");

        return endpoints;
    }
}
