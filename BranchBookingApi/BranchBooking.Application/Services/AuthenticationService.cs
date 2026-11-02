using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;

namespace BranchBooking.Application.Services;

public sealed class AuthenticationService(
    IAuthenticationRepository authenticationRepository,
    IPasswordService passwordService,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthenticationService
{
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaximumFailedLogins = 5;

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
        if (user is null || user.Status == UserStatus.Disabled || user.Status == UserStatus.Invited)
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
        if (user is null || user.Status is UserStatus.Disabled or UserStatus.Locked)
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
        await authenticationRepository.RotateRefreshSessionAsync(session.Id, replacementId, cancellationToken);

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
            throw new DomainValidationException("All staff invitation details are required.");
        }

        var email = NormalizeEmail(request.Email);
        if (await authenticationRepository.GetUserByEmailAsync(email, cancellationToken) is not null)
        {
            throw new DomainValidationException("An account with this email already exists.");
        }

        var token = tokenService.CreateRefreshToken();
        await authenticationRepository.CreateStaffInvitationAsync(
            assignedByUserId,
            request,
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
        user.Roles);

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
