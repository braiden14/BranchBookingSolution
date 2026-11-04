using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Infrastructure.Persistence;
using Dapper;
using Microsoft.Extensions.Logging;

namespace BranchBooking.Infrastructure.Authentication;

public sealed class BootstrapAdminSeeder(
    DbConnectionFactory connectionFactory,
    IPasswordService passwordService,
    ILogger<BootstrapAdminSeeder> logger)
{
    public async Task SeedAsync(BootstrapAdminOptions options, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return;
        }

        Validate(options);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var adminExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            """
            SELECT EXISTS (
                SELECT 1
                FROM public.user_roles ur
                JOIN public.roles r ON r.id = ur.role_id
                JOIN public.users u ON u.id = ur.user_id
                WHERE r.code = 'Admin'
                  AND ur.revoked_at_utc IS NULL
                  AND u.status = 1
            )
            """,
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (adminExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogInformation("Bootstrap admin seeding skipped because an active administrator already exists.");
            return;
        }

        var passwordHash = passwordService.Hash(options.Password!);
        var userId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO public.users
                (email, password_hash, first_name, last_name, status, email_confirmed)
            VALUES
                (CAST(@Email AS CITEXT), @PasswordHash, @FirstName, @LastName, 1, TRUE)
            RETURNING id
            """,
            new
            {
                Email = options.Email!.Trim(),
                PasswordHash = passwordHash,
                FirstName = options.FirstName!.Trim(),
                LastName = options.LastName!.Trim()
            },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO public.user_roles (user_id, role_id, assigned_by_user_id)
            SELECT @UserId, id, @UserId
            FROM public.roles
            WHERE code = 'Admin'
            """,
            new { UserId = userId },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Bootstrap administrator created for {Email}.", options.Email!.Trim());
    }

    private static void Validate(BootstrapAdminOptions options)
    {
        if (!options.Confirm)
        {
            throw new InvalidOperationException("Bootstrap admin seeding requires BootstrapAdmin:Confirm=true.");
        }

        if (string.IsNullOrWhiteSpace(options.Email) ||
            string.IsNullOrWhiteSpace(options.FirstName) ||
            string.IsNullOrWhiteSpace(options.LastName) ||
            string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException("Bootstrap admin email, name, and password must be configured.");
        }

        if (options.Password.Length < 12 ||
            !options.Password.Any(char.IsUpper) ||
            !options.Password.Any(char.IsLower) ||
            !options.Password.Any(char.IsDigit) ||
            !options.Password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new InvalidOperationException("Bootstrap admin password must be at least 12 characters and include upper-case, lower-case, numeric, and special characters.");
        }
    }
}
