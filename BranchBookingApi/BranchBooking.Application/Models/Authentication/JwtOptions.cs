namespace BranchBooking.Application.Models.Authentication;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "BranchBookingApi";

    public string Audience { get; set; } = "BookingUI";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 10;

    public int RefreshTokenDays { get; set; } = 30;
}
