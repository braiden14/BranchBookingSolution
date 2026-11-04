namespace BranchBooking.Infrastructure.Authentication;

public sealed class BootstrapAdminOptions
{
    public bool Enabled { get; set; }

    public bool Confirm { get; set; }

    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Password { get; set; }
}
