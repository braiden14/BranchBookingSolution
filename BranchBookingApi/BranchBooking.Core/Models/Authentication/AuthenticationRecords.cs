using BranchBooking.Core.Enums;

namespace BranchBooking.Core.Models.Authentication;

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
    IReadOnlyList<string> Roles,
    long? AssignedBranchId = null);

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
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc);

public sealed record StaffRecord(
    long UserId,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    UserStatus Status,
    IReadOnlyList<string> Roles,
    long? BranchId,
    string? BranchName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset? InvitationExpiresAtUtc);
