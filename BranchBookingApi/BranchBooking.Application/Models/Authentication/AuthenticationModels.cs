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
    long? BranchId);

public sealed record AcceptStaffInvitationRequest(
    string Token,
    string Password);

public sealed record UpdateStaffStatusRequest(UserStatus Status);

public sealed record AuthUserDto(
    long Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    long? AssignedBranchId);

public sealed record AuthenticationResult(
    AuthUserDto User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc);
