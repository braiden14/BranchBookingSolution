using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Application.DTOs;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;
using BranchBooking.Core.Models.Authentication;

namespace BranchBooking.Application.Services;

public sealed class AuthenticationService(
    IAuthenticationRepository authenticationRepository,
    IPasswordService passwordService,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthenticationService
{
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaximumFailedLogins = 5;

    public async Task<PagedStaffDto> GetStaffAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await authenticationRepository.GetStaffAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task UpdateStaffRoleAsync(long userId, string roleCode, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(roleCode))
        {
            throw new DomainValidationException("A valid staff member and role are required.");
        }

        var normalizedRoleCode = roleCode.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalizedRoleCode is not ("Admin" or "Staff" or "BranchManager"))
        {
            throw new DomainValidationException("The role must be Admin, Staff, or BranchManager.");
        }

        var user = await authenticationRepository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new DomainValidationException("The staff member could not be found.");
        if (!user.Roles.Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Staff", StringComparison.OrdinalIgnoreCase) || role.Equals("BranchManager", StringComparison.OrdinalIgnoreCase) || role.Equals("Branch Manager", StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException("Only staff accounts can be assigned staff roles.");
        }

        await authenticationRepository.UpdateStaffRoleAsync(userId, normalizedRoleCode, cancellationToken);
    }

    public async Task UpdateStaffStatusAsync(long userId, UserStatus status, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || status is not (UserStatus.Active or UserStatus.Disabled))
        {
            throw new DomainValidationException("A valid staff member and active or disabled status are required.");
        }

        var user = await authenticationRepository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new DomainValidationException("The staff member could not be found.");
        if (!user.Roles.Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Staff", StringComparison.OrdinalIgnoreCase) || role.Equals("BranchManager", StringComparison.OrdinalIgnoreCase) || role.Equals("Branch Manager", StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException("Only Admin, Staff, and BranchManager accounts can be managed here.");
        }

        await authenticationRepository.UpdateStaffStatusAsync(userId, status, cancellationToken);
        if (status == UserStatus.Disabled)
        {
            await authenticationRepository.RevokeUserSessionsAsync(userId, "Staff account deactivated", cancellationToken);
        }
    }

    public async Task<AuthenticationResult> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRegistration(request);
        var email = NormalizeEmail(request.Email);
        var existing = await authenticationRepository.GetUserByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new DomainValidationException("An account with this email already exists.");
        }

        var userId = await authenticationRepository.CreateCustomerAsync(request, email, passwordService.Hash(request.Password), cancellationToken);
        var user = await authenticationRepository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new AuthenticationException("The customer account could not be loaded after creation.");
        return CreateAuthenticationResult(user);
    }

    public async Task<(AuthenticationResult Result, string RefreshToken)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        var user = await authenticationRepository.GetUserByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user is null || user.Status is UserStatus.Disabled or UserStatus.Locked or UserStatus.Invited)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        var now = timeProvider.GetUtcNow();
        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc > now)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        if (!passwordService.Verify(request.Password, user.PasswordHash))
        {
            var failedCount = user.FailedLoginCount + 1;
            var lockedUntil = failedCount >= MaximumFailedLogins ? now.Add(LockoutDuration) : (DateTimeOffset?)null;
            await authenticationRepository.RecordFailedLoginAsync(user.Id, failedCount, lockedUntil, cancellationToken);
            throw new AuthenticationException("Invalid email or password.");
        }

        await authenticationRepository.RecordSuccessfulLoginAsync(user.Id, cancellationToken);
        return await CreateAuthenticationResultWithRefreshAsync(user, cancellationToken);
    }

    public async Task<(AuthenticationResult Result, string RefreshToken)> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new AuthenticationException("The refresh session is invalid.");
        }

        var session = await authenticationRepository.GetRefreshSessionAsync(tokenService.HashRefreshToken(refreshToken), cancellationToken);
        if (session is null)
        {
            throw new AuthenticationException("The refresh session is invalid.");
        }

        var now = timeProvider.GetUtcNow();
        if (session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= now)
        {
            if (session.RevokedAtUtc.HasValue && session.ReplacedBySessionId.HasValue)
            {
                await authenticationRepository.RevokeRefreshSessionFamilyAsync(session.FamilyId, "Refresh token reuse detected", cancellationToken);
            }

            throw new AuthenticationException("The refresh session is invalid.");
        }

        var user = await authenticationRepository.GetUserByIdAsync(session.UserId, cancellationToken);
        if (user is null || user.Status is UserStatus.Disabled or UserStatus.Locked || user.LockedUntilUtc > now)
        {
            throw new AuthenticationException("The refresh session is invalid.");
        }

        var replacementToken = tokenService.CreateRefreshToken();
        var replacementId = await authenticationRepository.CreateRefreshSessionAsync(
            user.Id,
            session.FamilyId,
            tokenService.HashRefreshToken(replacementToken),
            now.AddDays(30),
            cancellationToken);
        if (!await authenticationRepository.RotateRefreshSessionAsync(session.Id, replacementId, cancellationToken))
        {
            await authenticationRepository.RevokeRefreshSessionFamilyAsync(session.FamilyId, "Refresh token reuse detected", cancellationToken);
            throw new AuthenticationException("The refresh session is invalid.");
        }

        return (CreateAuthenticationResult(user), replacementToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var session = await authenticationRepository.GetRefreshSessionAsync(tokenService.HashRefreshToken(refreshToken), cancellationToken);
        if (session is not null)
        {
            await authenticationRepository.RevokeRefreshSessionAsync(session.Id, "Logout", cancellationToken);
        }
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await authenticationRepository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new AuthenticationException("The account could not be found.");
        return ToDto(user);
    }

    public async Task<string> InviteStaffAsync(long assignedByUserId, InviteStaffRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.RoleCode))
        {
            throw new DomainValidationException("All staff invitation details and a valid branch are required.");
        }

        var roleCode = request.RoleCode.Trim();
        var normalizedRoleCode = roleCode.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!normalizedRoleCode.Equals("Staff", StringComparison.OrdinalIgnoreCase) &&
            !normalizedRoleCode.Equals("BranchManager", StringComparison.OrdinalIgnoreCase) &&
            !normalizedRoleCode.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Invitations may only use the Admin, Staff, or BranchManager roles.");
        }

        if (!normalizedRoleCode.Equals("Admin", StringComparison.OrdinalIgnoreCase) && (request.BranchId is null or <= 0))
        {
            throw new DomainValidationException("A valid branch is required for Staff and BranchManager invitations.");
        }

        var normalizedRequest = request with { RoleCode = normalizedRoleCode, BranchId = normalizedRoleCode.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? null : request.BranchId };

        var email = NormalizeEmail(request.Email);
        if (await authenticationRepository.GetUserByEmailAsync(email, cancellationToken) is not null)
        {
            throw new DomainValidationException("An account with this email already exists.");
        }

        var token = tokenService.CreateRefreshToken();
        await authenticationRepository.CreateStaffInvitationAsync(
            assignedByUserId,
            normalizedRequest,
            email,
            tokenService.HashRefreshToken(token),
            timeProvider.GetUtcNow().AddDays(2),
            cancellationToken);
        return token;
    }

    public async Task AcceptStaffInvitationAsync(AcceptStaffInvitationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainValidationException("A valid invitation token and password are required.");
        }

        var invitation = await authenticationRepository.GetStaffInvitationAsync(tokenService.HashRefreshToken(request.Token), cancellationToken);
        if (invitation is null || invitation.AcceptedAtUtc.HasValue || invitation.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            throw new AuthenticationException("The staff invitation is invalid or expired.");
        }

        await authenticationRepository.AcceptStaffInvitationAsync(
            invitation.Id,
            invitation.UserId,
            passwordService.Hash(request.Password),
            cancellationToken);
    }

    private async Task<(AuthenticationResult Result, string RefreshToken)> CreateAuthenticationResultWithRefreshAsync(AuthUserRecord user, CancellationToken cancellationToken)
    {
        var authenticationResult = CreateAuthenticationResult(user);
        var refreshToken = tokenService.CreateRefreshToken();
        await authenticationRepository.CreateRefreshSessionAsync(
            user.Id,
            Guid.NewGuid(),
            tokenService.HashRefreshToken(refreshToken),
            timeProvider.GetUtcNow().AddDays(30),
            cancellationToken);
        return (authenticationResult, refreshToken);
    }

    private AuthenticationResult CreateAuthenticationResult(AuthUserRecord user)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        return new AuthenticationResult(ToDto(user), accessToken.Token, accessToken.ExpiresAtUtc);
    }

    private static AuthUserDto ToDto(AuthUserRecord user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.PhoneNumber,
        user.Roles,
        user.AssignedBranchId);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static void ValidateRegistration(RegisterCustomerRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new DomainValidationException("Email, first name, and last name are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new DomainValidationException("Password must be at least 8 characters long.");
        }
    }
}
