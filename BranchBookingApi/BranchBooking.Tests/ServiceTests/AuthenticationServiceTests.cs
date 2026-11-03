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

    [Test]
    public async Task LoginAsync_WhenPasswordIsValid_RecordsSuccessCreatesSessionAndReturnsTokens()
    {
        var user = CreateUser();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        _repository.GetUserByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwords.Verify("correct", user.PasswordHash).Returns(true);
        _tokens.CreateAccessToken(user).Returns(("access-token", expiresAt));
        _tokens.CreateRefreshToken().Returns("refresh-token");
        _tokens.HashRefreshToken("refresh-token").Returns("refresh-hash");
        _repository.CreateRefreshSessionAsync(5, Arg.Any<Guid>(), "refresh-hash", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(12L);

        var result = await _service.LoginAsync(new LoginRequest(" USER@example.com ", "correct"));

        Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));
        Assert.That(result.Result.AccessToken, Is.EqualTo("access-token"));
        Assert.That(result.Result.User.Roles, Is.EqualTo(new[] { "Customer" }));
        await _repository.Received(1).RecordSuccessfulLoginAsync(5, Arg.Any<CancellationToken>());
        await _repository.Received(1).CreateRefreshSessionAsync(5, Arg.Any<Guid>(), "refresh-hash", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LoginAsync_OnFifthFailedAttempt_SetsFifteenMinuteLockout()
    {
        var user = CreateUser() with { FailedLoginCount = 4 };
        _repository.GetUserByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwords.Verify("wrong", user.PasswordHash).Returns(false);

        var exception = Assert.ThrowsAsync<AuthenticationException>(async () =>
            await _service.LoginAsync(new LoginRequest("user@example.com", "wrong")));

        Assert.That(exception!.Message, Is.EqualTo("Invalid email or password."));

        await _repository.Received(1).RecordFailedLoginAsync(
            5,
            5,
            Arg.Is<DateTimeOffset?>(value => value.HasValue && value.Value > DateTimeOffset.UtcNow.AddMinutes(14) && value.Value <= DateTimeOffset.UtcNow.AddMinutes(16)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefreshAsync_WhenSessionIsValid_CreatesReplacementAndRotatesSession()
    {
        var session = new RefreshSessionRecord(9, 5, Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(1), null, null);
        var user = CreateUser();
        _tokens.HashRefreshToken("refresh").Returns("hash");
        _tokens.CreateRefreshToken().Returns("replacement");
        _tokens.HashRefreshToken("replacement").Returns("replacement-hash");
        _repository.GetRefreshSessionAsync("hash", Arg.Any<CancellationToken>()).Returns(session);
        _repository.GetUserByIdAsync(5, Arg.Any<CancellationToken>()).Returns(user);
        _tokens.CreateAccessToken(user).Returns(("access-token", DateTimeOffset.UtcNow.AddMinutes(10)));
        _repository.CreateRefreshSessionAsync(5, session.FamilyId, "replacement-hash", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(10L);

        var result = await _service.RefreshAsync("refresh");

        Assert.That(result.RefreshToken, Is.EqualTo("replacement"));
        Assert.That(result.Result.AccessToken, Is.EqualTo("access-token"));
        await _repository.Received(1).RotateRefreshSessionAsync(session.Id, 10L, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LogoutAsync_WhenSessionExists_RevokesSession()
    {
        var session = new RefreshSessionRecord(9, 5, Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(1), null, null);
        _tokens.HashRefreshToken("refresh").Returns("hash");
        _repository.GetRefreshSessionAsync("hash", Arg.Any<CancellationToken>()).Returns(session);

        await _service.LogoutAsync("refresh");

        await _repository.Received(1).RevokeRefreshSessionAsync(9, "Logout", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RegisterCustomerAsync_WhenEmailExists_ThrowsBeforeCreatingCustomer()
    {
        _repository.GetUserByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(CreateUser());

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () =>
            await _service.RegisterCustomerAsync(new RegisterCustomerRequest(" USER@example.com ", "password1", "Jane", "Doe", null)));

        Assert.That(exception!.Message, Is.EqualTo("An account with this email already exists."));
        await _repository.DidNotReceive().CreateCustomerAsync(Arg.Any<RegisterCustomerRequest>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AcceptStaffInvitationAsync_WhenValid_HashesPasswordAndAcceptsInvitation()
    {
        var invitation = new StaffInvitationRecord(8, 5, "token-hash", DateTimeOffset.UtcNow.AddDays(1), null);
        _tokens.HashRefreshToken("invite").Returns("token-hash");
        _passwords.Hash("password1").Returns("password-hash");
        _repository.GetStaffInvitationAsync("token-hash", Arg.Any<CancellationToken>()).Returns(invitation);

        await _service.AcceptStaffInvitationAsync(new AcceptStaffInvitationRequest("invite", "password1"));

        await _repository.Received(1).AcceptStaffInvitationAsync(8, 5, "password-hash", Arg.Any<CancellationToken>());
    }

    private static AuthUserRecord CreateUser() =>
        new(5, "user@example.com", "stored", "Jane", "Doe", null, UserStatus.Active, true, 0, null, 1, ["Customer"]);
}
