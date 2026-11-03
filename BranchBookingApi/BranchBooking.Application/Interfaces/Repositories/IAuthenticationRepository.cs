using BranchBooking.Application.Models.Authentication;
using BranchBooking.Core.Models.Authentication;
using BranchBooking.Application.DTOs;
using BranchBooking.Core.Enums;

namespace BranchBooking.Application.Interfaces.Repositories;

public interface IAuthenticationRepository
{
    Task<PagedStaffDto> GetStaffAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task UpdateStaffStatusAsync(long userId, UserStatus status, CancellationToken cancellationToken = default);

    Task RevokeUserSessionsAsync(long userId, string reason, CancellationToken cancellationToken = default);

    Task<AuthUserRecord?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<AuthUserRecord?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);

    Task<long?> GetStaffBranchIdAsync(long userId, CancellationToken cancellationToken = default);

    Task<long> CreateCustomerAsync(
        RegisterCustomerRequest request,
        string normalizedEmail,
        string passwordHash,
        CancellationToken cancellationToken = default);

    Task<long> CreateStaffInvitationAsync(
        long assignedByUserId,
        InviteStaffRequest request,
        string normalizedEmail,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<StaffInvitationRecord?> GetStaffInvitationAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AcceptStaffInvitationAsync(
        long invitationId,
        long userId,
        string passwordHash,
        CancellationToken cancellationToken = default);

    Task RecordFailedLoginAsync(
        long userId,
        int failedLoginCount,
        DateTimeOffset? lockedUntilUtc,
        CancellationToken cancellationToken = default);

    Task RecordSuccessfulLoginAsync(long userId, CancellationToken cancellationToken = default);

    Task<RefreshSessionRecord?> GetRefreshSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<long> CreateRefreshSessionAsync(
        long userId,
        Guid familyId,
        string refreshTokenHash,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task RotateRefreshSessionAsync(
        long sessionId,
        long replacementSessionId,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshSessionAsync(
        long sessionId,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshSessionFamilyAsync(
        Guid familyId,
        string reason,
        CancellationToken cancellationToken = default);
}
