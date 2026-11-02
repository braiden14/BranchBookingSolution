using BranchBooking.Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace BranchBooking.Infrastructure.Authentication;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Marker = new();

    public string Hash(string password) => _hasher.HashPassword(Marker, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(Marker, passwordHash, password) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
