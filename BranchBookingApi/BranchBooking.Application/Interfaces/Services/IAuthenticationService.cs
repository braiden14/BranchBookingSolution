using BranchBooking.Application.Models.Authentication;
using BranchBooking.Application.DTOs;
using BranchBooking.Core.Enums;

namespace BranchBooking.Application.Interfaces.Services;

public interface IAuthenticationService
{
    Task<PagedStaffDto> GetStaffAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task UpdateStaffStatusAsync(long userId, UserStatus status, CancellationToken cancellationToken = default);

    Task UpdateStaffRoleAsync(long userId, string roleCode, CancellationToken cancellationToken = default);

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
