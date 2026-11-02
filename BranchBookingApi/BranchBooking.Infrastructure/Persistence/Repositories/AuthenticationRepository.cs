using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Core.Enums;
using Dapper;
using System.Data;

namespace BranchBooking.Infrastructure.Persistence.Repositories;

public sealed class AuthenticationRepository(DbConnectionFactory connectionFactory) : IAuthenticationRepository
{
    public async Task<AuthUserRecord?> GetUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await QueryUserAsync(connection, "WHERE u.email = @Email", new { Email = normalizedEmail }, cancellationToken);
    }

    public async Task<AuthUserRecord?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await QueryUserAsync(connection, "WHERE u.id = @UserId", new { UserId = userId }, cancellationToken);
    }

    public async Task<long> CreateCustomerAsync(RegisterCustomerRequest request, string normalizedEmail, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var userId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO public.users (email, password_hash, first_name, last_name, phone_number, status, email_confirmed)
            VALUES (@Email, @PasswordHash, @FirstName, @LastName, @PhoneNumber, 1, FALSE)
            RETURNING id
            """,
            new
            {
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                request.FirstName,
                request.LastName,
                request.PhoneNumber
            }, transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO public.customer_profiles (user_id) VALUES (@UserId); INSERT INTO public.user_roles (user_id, role_id) SELECT @UserId, id FROM public.roles WHERE code = 'Customer';",
            new { UserId = userId }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return userId;
    }

    public async Task<long> CreateStaffInvitationAsync(long assignedByUserId, InviteStaffRequest request, string normalizedEmail, string tokenHash, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var userId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO public.users (email, password_hash, first_name, last_name, phone_number, status, email_confirmed)
            VALUES (@Email, '', @FirstName, @LastName, @PhoneNumber, 4, FALSE)
            RETURNING id
            """,
            new
            {
                Email = normalizedEmail,
                request.FirstName,
                request.LastName,
                request.PhoneNumber
            }, transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO public.staff_profiles (user_id) VALUES (@UserId);
            INSERT INTO public.user_roles (user_id, role_id, assigned_by_user_id)
            SELECT @UserId, id, @AssignedByUserId FROM public.roles WHERE code = @RoleCode;
            INSERT INTO public.staff_invitations (user_id, token_hash, role_id, invited_by_user_id, expires_at_utc)
            SELECT @UserId, @TokenHash, id, @AssignedByUserId, @ExpiresAtUtc
            FROM public.roles WHERE code = @RoleCode
            RETURNING id;
            """,
            new
            {
                UserId = userId,
                AssignedByUserId = assignedByUserId,
                RoleCode = request.RoleCode,
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
            "SELECT id, user_id AS UserId, token_hash AS TokenHash, expires_at_utc AS ExpiresAtUtc, accepted_at_utc AS AcceptedAtUtc FROM public.staff_invitations WHERE token_hash = @TokenHash",
            new { TokenHash = tokenHash }, commandType: CommandType.Text, cancellationToken: cancellationToken));
    }

    public async Task AcceptStaffInvitationAsync(long invitationId, long userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE public.users SET password_hash = @PasswordHash, status = 1, email_confirmed = TRUE, updated_at_utc = now() WHERE id = @UserId; UPDATE public.staff_invitations SET accepted_at_utc = now() WHERE id = @InvitationId AND accepted_at_utc IS NULL;",
            new { UserId = userId, InvitationId = invitationId, PasswordHash = passwordHash }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RecordFailedLoginAsync(long userId, int failedLoginCount, DateTimeOffset? lockedUntilUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE public.users SET failed_login_count = @FailedLoginCount, locked_until_utc = @LockedUntilUtc, updated_at_utc = now() WHERE id = @UserId",
            new { UserId = userId, FailedLoginCount = failedLoginCount, LockedUntilUtc = lockedUntilUtc }, cancellationToken: cancellationToken));
    }

    public async Task RecordSuccessfulLoginAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE public.users SET failed_login_count = 0, locked_until_utc = NULL, last_login_at_utc = now(), updated_at_utc = now() WHERE id = @UserId",
            new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<RefreshSessionRecord?> GetRefreshSessionAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RefreshSessionRecord>(new CommandDefinition(
            "SELECT id, user_id AS UserId, family_id AS FamilyId, refresh_token_hash AS RefreshTokenHash, expires_at_utc AS ExpiresAtUtc, revoked_at_utc AS RevokedAtUtc, replaced_by_session_id AS ReplacedBySessionId FROM public.user_sessions WHERE refresh_token_hash = @RefreshTokenHash",
            new { RefreshTokenHash = refreshTokenHash }, cancellationToken: cancellationToken));
    }

    public async Task<long> CreateRefreshSessionAsync(long userId, Guid familyId, string refreshTokenHash, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO public.user_sessions (user_id, family_id, refresh_token_hash, expires_at_utc) VALUES (@UserId, @FamilyId, @RefreshTokenHash, @ExpiresAtUtc) RETURNING id",
            new { UserId = userId, FamilyId = familyId, RefreshTokenHash = refreshTokenHash, ExpiresAtUtc = expiresAtUtc }, cancellationToken: cancellationToken));
    }

    public async Task RotateRefreshSessionAsync(long sessionId, long replacementSessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE public.user_sessions SET revoked_at_utc = now(), revoke_reason = 'Rotated', replaced_by_session_id = @ReplacementSessionId, last_used_at_utc = now() WHERE id = @SessionId AND revoked_at_utc IS NULL",
            new { SessionId = sessionId, ReplacementSessionId = replacementSessionId }, cancellationToken: cancellationToken));
    }

    public async Task RevokeRefreshSessionAsync(long sessionId, string reason, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE public.user_sessions SET revoked_at_utc = COALESCE(revoked_at_utc, now()), revoke_reason = @Reason WHERE id = @SessionId",
            new { SessionId = sessionId, Reason = reason }, cancellationToken: cancellationToken));
    }

    public async Task RevokeRefreshSessionFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE public.user_sessions SET revoked_at_utc = COALESCE(revoked_at_utc, now()), revoke_reason = @Reason WHERE family_id = @FamilyId",
            new { FamilyId = familyId, Reason = reason }, cancellationToken: cancellationToken));
    }

    private static async Task<AuthUserRecord?> QueryUserAsync(IDbConnection connection, string predicate, object parameters, CancellationToken cancellationToken)
    {
        const string select = """
            SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.phone_number,
                   u.status, u.email_confirmed, u.failed_login_count, u.locked_until_utc,
                   u.authorization_version,
                   COALESCE(array_agg(r.code) FILTER (WHERE r.code IS NOT NULL), ARRAY[]::varchar[]) AS roles
            FROM public.users u
            LEFT JOIN public.user_roles ur ON ur.user_id = u.id AND ur.revoked_at_utc IS NULL
            LEFT JOIN public.roles r ON r.id = ur.role_id
            """;
        var sql = $"{select} {predicate} GROUP BY u.id";
        var row = await connection.QuerySingleOrDefaultAsync<AuthUserDbRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return row is null
            ? null
            : new AuthUserRecord(row.id, row.email, row.password_hash, row.first_name, row.last_name, row.phone_number,
                (UserStatus)row.status, row.email_confirmed, row.failed_login_count, row.locked_until_utc,
                row.authorization_version, row.roles);
    }

    private sealed record AuthUserDbRow(
        long id,
        string email,
        string password_hash,
        string first_name,
        string last_name,
        string? phone_number,
        short status,
        bool email_confirmed,
        int failed_login_count,
        DateTimeOffset? locked_until_utc,
        int authorization_version,
        string[] roles);
}
