using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Authentication;
using BranchBooking.Core.Models.Authentication;
using BranchBooking.Application.Services;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests;

[TestFixture]
public sealed class AuthenticationServiceTests
{
    private IAuthenticationRepository _repository = null!;
    private IPasswordService _passwords = null!;
    private ITokenService _tokens = null!;
    private AuthenticationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IAuthenticationRepository>();
        _passwords = Substitute.For<IPasswordService>();
        _tokens = Substitute.For<ITokenService>();
        _service = new AuthenticationService(_repository, _passwords, _tokens, TimeProvider.System);
    }

    [Test]
    public void RegisterCustomerAsync_WhenPasswordIsTooShort_RejectsBeforePersistence()
    {
        var request = new RegisterCustomerRequest("user@example.com", "short", "Jane", "Doe", null);

        var exception = Assert.ThrowsAsync<DomainValidationException>(
            async () => await _service.RegisterCustomerAsync(request));

        Assert.That(exception!.Message, Is.EqualTo("Password must be at least 8 characters long."));
        _repository.DidNotReceive().CreateCustomerAsync(
            Arg.Any<RegisterCustomerRequest>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LoginAsync_WhenPasswordFails_RecordsFailureAndDoesNotCreateSession()
    {
        var user = new AuthUserRecord(5, "user@example.com", "stored", "Jane", "Doe", null, UserStatus.Active, true, 4, null, 1, ["Customer"]);
        _repository.GetUserByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwords.Verify("wrong", "stored").Returns(false);

        var exception = Assert.ThrowsAsync<AuthenticationException>(
            async () => await _service.LoginAsync(new LoginRequest(" USER@example.com ", "wrong")));

        Assert.That(exception!.Message, Is.EqualTo("Invalid email or password."));
        await _repository.Received(1).RecordFailedLoginAsync(5, 5, Arg.Is<DateTimeOffset?>(value => value.HasValue), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().CreateRefreshSessionAsync(
            Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefreshAsync_WhenRotatedTokenIsReused_RevokesTokenFamily()
    {
        var session = new RefreshSessionRecord(9, 5, Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddMinutes(-1), 10);
        _tokens.HashRefreshToken("refresh").Returns("hash");
        _repository.GetRefreshSessionAsync("hash", Arg.Any<CancellationToken>()).Returns(session);

        var exception = Assert.ThrowsAsync<AuthenticationException>(
            async () => await _service.RefreshAsync("refresh"));

        Assert.That(exception!.Message, Is.EqualTo("The refresh session is invalid."));
        await _repository.Received(1).RevokeRefreshSessionFamilyAsync(session.FamilyId, "Refresh token reuse detected", Arg.Any<CancellationToken>());
    }
}
