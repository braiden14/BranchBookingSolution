using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BranchBooking.Api.Endpoints;

public static class AuthenticationEndpoints
{
    private const string RefreshCookie = "branchbook_refresh";

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegisterCustomerRequest request, IAuthenticationService authenticationService, CancellationToken cancellationToken) =>
        {
            var result = await authenticationService.RegisterCustomerAsync(request, cancellationToken);
            return Results.Created("/api/auth/me", result);
        });

        group.MapPost("/login", async (LoginRequest request, IAuthenticationService authenticationService, HttpResponse response, CancellationToken cancellationToken) =>
        {
            var authentication = await authenticationService.LoginAsync(request, cancellationToken);
            SetRefreshCookie(response, authentication.RefreshToken);
            return Results.Ok(authentication.Result);
        });

        group.MapPost("/refresh", async (IAuthenticationService authenticationService, HttpRequest request, HttpResponse response, CancellationToken cancellationToken) =>
        {
            if (!request.Cookies.TryGetValue(RefreshCookie, out var refreshToken)) return Results.Unauthorized();
            var authentication = await authenticationService.RefreshAsync(refreshToken, cancellationToken);
            SetRefreshCookie(response, authentication.RefreshToken);
            return Results.Ok(authentication.Result);
        });

        group.MapPost("/logout", async (IAuthenticationService authenticationService, HttpRequest request, HttpResponse response, CancellationToken cancellationToken) =>
        {
            if (request.Cookies.TryGetValue(RefreshCookie, out var refreshToken))
            {
                await authenticationService.LogoutAsync(refreshToken, cancellationToken);
            }
            response.Cookies.Delete(RefreshCookie, CookieOptions());
            return Results.NoContent();
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, IAuthenticationService authenticationService, CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            return long.TryParse(subject, out var userId)
                ? Results.Ok(await authenticationService.GetCurrentUserAsync(userId, cancellationToken))
                : Results.Unauthorized();
        }).RequireAuthorization();

        group.MapPost("/staff/invitations", async (InviteStaffRequest request, ClaimsPrincipal principal, IAuthenticationService authenticationService, CancellationToken cancellationToken) =>
        {
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            if (!long.TryParse(subject, out var assignedByUserId)) return Results.Unauthorized();
            var invitationToken = await authenticationService.InviteStaffAsync(assignedByUserId, request, cancellationToken);
            return Results.Ok(new { invitationToken });
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/staff", async (int? pageNumber, int? pageSize, IAuthenticationService authenticationService, CancellationToken cancellationToken) =>
        {
            var result = await authenticationService.GetStaffAsync(pageNumber ?? 1, pageSize ?? 20, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        group.MapPatch("/staff/{userId:long}/status", async (long userId, UpdateStaffStatusRequest request, IAuthenticationService authenticationService, CancellationToken cancellationToken) =>
        {
            await authenticationService.UpdateStaffStatusAsync(userId, request.Status, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/staff/invitations/accept", async (AcceptStaffInvitationRequest request, IAuthenticationService authenticationService, CancellationToken cancellationToken) =>
        {
            await authenticationService.AcceptStaffInvitationAsync(request, cancellationToken);
            return Results.NoContent();
        });

        return endpoints;
    }

    private static void SetRefreshCookie(HttpResponse response, string refreshToken) => response.Cookies.Append(RefreshCookie, refreshToken, CookieOptions());

    private static CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/api/auth",
        MaxAge = TimeSpan.FromDays(30)
    };
}
