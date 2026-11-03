using BranchBooking.Core.Enums;

namespace BranchBooking.Application.Models.Authentication;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterCustomerRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber);

public sealed record InviteStaffRequest(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string RoleCode,
    long BranchId);

public sealed record AcceptStaffInvitationRequest(
    string Token,
    string Password);

public sealed record AuthUserRecord(
    long Id,
    string Email,
    string PasswordHash,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserStatus Status,
    bool EmailConfirmed,
    int FailedLoginCount,
    DateTimeOffset? LockedUntilUtc,
    int AuthorizationVersion,
    IReadOnlyList<string> Roles);

public sealed record AuthUserDto(
    long Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles);

public sealed record AuthenticationResult(
    AuthUserDto User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);

public sealed record RefreshSessionRecord(
    long Id,
    long UserId,
    Guid FamilyId,
    string RefreshTokenHash,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    long? ReplacedBySessionId);

public sealed record StaffInvitationRecord(
    long Id,
    long UserId,
    string TokenHash,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc);
