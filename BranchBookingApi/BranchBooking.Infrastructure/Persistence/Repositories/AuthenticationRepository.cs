using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Models.Authentication;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class AuthenticationRepository(DbConnectionFactory connectionFactory) : IAuthenticationRepository
{
    public async Task<AuthUserRecord?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await QueryUserAsync(connection, "SELECT * FROM public.get_auth_user_by_email(@Email)", new { Email = normalizedEmail }, cancellationToken);
    }

    public async Task<AuthUserRecord?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await QueryUserAsync(connection, "SELECT * FROM public.get_auth_user_by_id(@UserId)", new { UserId = userId }, cancellationToken);
    }

    public async Task<long?> GetStaffBranchIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            "SELECT public.get_staff_branch_id(@UserId)",
            new { UserId = userId },
            cancellationToken: cancellationToken));
    }

    public async Task<long> CreateCustomerAsync(RegisterCustomerRequest request, string normalizedEmail, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var userId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT public.create_customer(@Email, @PasswordHash, @FirstName, @LastName, @PhoneNumber)",
            new
            {
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                request.FirstName,
                request.LastName,
                request.PhoneNumber
            }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return userId;
    }

    public async Task<long> CreateStaffInvitationAsync(long assignedByUserId, InviteStaffRequest request, string normalizedEmail, string tokenHash, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var userId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT public.create_staff_invitation(@Email, @FirstName, @LastName, @PhoneNumber, @BranchId, @AssignedByUserId, @RoleCode, @TokenHash, @ExpiresAtUtc)",
            new
            {
                Email = normalizedEmail,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.BranchId,
                AssignedByUserId = assignedByUserId,
                request.RoleCode,
                TokenHash = tokenHash,
                ExpiresAtUtc = expiresAtUtc
            }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return userId;
    }

    public async Task<StaffInvitationRecord?> GetStaffInvitationAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<StaffInvitationRecord>(new CommandDefinition(
            "SELECT * FROM public.get_staff_invitation(@TokenHash)",
            new { TokenHash = tokenHash }, commandType: CommandType.Text, cancellationToken: cancellationToken));
    }

    public async Task AcceptStaffInvitationAsync(long invitationId, long userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT public.accept_staff_invitation(@InvitationId, @UserId, @PasswordHash)",
            new { UserId = userId, InvitationId = invitationId, PasswordHash = passwordHash }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RecordFailedLoginAsync(long userId, int failedLoginCount, DateTimeOffset? lockedUntilUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT public.record_failed_login(@UserId, @FailedLoginCount, @LockedUntilUtc)",
            new { UserId = userId, FailedLoginCount = failedLoginCount, LockedUntilUtc = lockedUntilUtc }, cancellationToken: cancellationToken));
    }

    public async Task RecordSuccessfulLoginAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT public.record_successful_login(@UserId)",
            new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<RefreshSessionRecord?> GetRefreshSessionAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RefreshSessionRecord>(new CommandDefinition(
            "SELECT * FROM public.get_refresh_session(@RefreshTokenHash)",
            new { RefreshTokenHash = refreshTokenHash }, cancellationToken: cancellationToken));
    }

    public async Task<long> CreateRefreshSessionAsync(long userId, Guid familyId, string refreshTokenHash, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT public.create_refresh_session(@UserId, @FamilyId, @RefreshTokenHash, @ExpiresAtUtc)",
            new { UserId = userId, FamilyId = familyId, RefreshTokenHash = refreshTokenHash, ExpiresAtUtc = expiresAtUtc }, cancellationToken: cancellationToken));
    }

    public async Task RotateRefreshSessionAsync(long sessionId, long replacementSessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT public.rotate_refresh_session(@SessionId, @ReplacementSessionId)",
            new { SessionId = sessionId, ReplacementSessionId = replacementSessionId }, cancellationToken: cancellationToken));
    }

    public async Task RevokeRefreshSessionAsync(long sessionId, string reason, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT public.revoke_refresh_session(@SessionId, @Reason)",
            new { SessionId = sessionId, Reason = reason }, cancellationToken: cancellationToken));
    }

    public async Task RevokeRefreshSessionFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "SELECT public.revoke_refresh_session_family(@FamilyId, @Reason)",
            new { FamilyId = familyId, Reason = reason }, cancellationToken: cancellationToken));
    }

    private static async Task<AuthUserRecord?> QueryUserAsync(IDbConnection connection, string sql, object parameters, CancellationToken cancellationToken)
    {
        var row = await connection.QuerySingleOrDefaultAsync<AuthUser>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return row is null
            ? null
            : new AuthUserRecord(row.id, row.email, row.password_hash, row.first_name, row.last_name, row.phone_number,
                (UserStatus)row.status, row.email_confirmed, row.failed_login_count, row.locked_until_utc,
                row.authorization_version, row.roles.Cast<string>().ToArray());
    }

    private sealed record AuthUser(
        long id,
        string email,
        string password_hash,
        string first_name,
        string last_name,
        string? phone_number,
        short status,
        bool email_confirmed,
        int failed_login_count,
        DateTime? locked_until_utc,
        int authorization_version,
        Array roles);
}
