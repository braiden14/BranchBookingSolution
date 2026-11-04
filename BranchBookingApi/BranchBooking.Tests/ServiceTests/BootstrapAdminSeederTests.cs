using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Infrastructure.Authentication;
using BranchBooking.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests;

[TestFixture]
public sealed class BootstrapAdminSeederTests
{
    [Test]
    public async Task SeedAsync_WhenDisabled_DoesNotRequireCredentialsOrDatabase()
    {
        var connectionFactory = Substitute.For<DbConnectionFactory>("Host=unused");
        var passwordService = Substitute.For<IPasswordService>();
        var logger = Substitute.For<ILogger<BootstrapAdminSeeder>>();
        var seeder = new BootstrapAdminSeeder(connectionFactory, passwordService, logger);

        Assert.DoesNotThrowAsync(() => seeder.SeedAsync(new BootstrapAdminOptions()));
    }

    [TestCase(false, "Bootstrap admin seeding requires BootstrapAdmin:Confirm=true.", null)]
    [TestCase(true, "Bootstrap admin email, name, and password must be configured.", null)]
    [TestCase(true, "Bootstrap admin password must be at least 12 characters and include upper-case, lower-case, numeric, and special characters.", "short")]
    public void SeedAsync_WhenConfigurationIsInvalid_Throws(bool confirm, string message, string? password)
    {
        var options = new BootstrapAdminOptions
        {
            Enabled = true,
            Confirm = confirm,
            Email = confirm ? "admin@example.com" : null,
            FirstName = confirm ? "System" : null,
            LastName = confirm ? "Administrator" : null,
            Password = password
        };

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var seeder = new BootstrapAdminSeeder(
                Substitute.For<DbConnectionFactory>("Host=unused"),
                Substitute.For<IPasswordService>(),
                Substitute.For<ILogger<BootstrapAdminSeeder>>());
            await seeder.SeedAsync(options);
        });

        Assert.That(exception!.Message, Is.EqualTo(message));
    }
}
