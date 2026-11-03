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
    IReadOnlyList<string> Roles);

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
