using BranchBooking.Application.Models.Authentication;

namespace BranchBooking.Application.Interfaces.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterCustomerAsync(
        RegisterCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<(AuthenticationResult Result, string RefreshToken)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<(AuthenticationResult Result, string RefreshToken)> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<AuthUserDto> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);

    Task<string> InviteStaffAsync(
        long assignedByUserId,
        InviteStaffRequest request,
        CancellationToken cancellationToken = default);

    Task AcceptStaffInvitationAsync(
        AcceptStaffInvitationRequest request,
        CancellationToken cancellationToken = default);
}
