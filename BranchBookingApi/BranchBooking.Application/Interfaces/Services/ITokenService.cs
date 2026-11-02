using BranchBooking.Application.Models.Authentication;

namespace BranchBooking.Application.Interfaces.Services;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(AuthUserRecord user);

    string CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
