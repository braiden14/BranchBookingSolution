using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Services;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests;

[TestFixture]
public sealed class BranchAccessServiceTests
{
    private IAuthenticationRepository _repository = null!;
    private BranchAccessService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IAuthenticationRepository>();
        _service = new BranchAccessService(_repository);
    }

    [Test]
    public async Task AdminCanReadAnyBranch()
    {
        Assert.That(await _service.CanReadBranchAsync(1, true, false, 99), Is.True);
        await _repository.DidNotReceive().GetStaffBranchIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StaffCanReadAssignedBranch()
    {
        _repository.GetStaffBranchIdAsync(7, Arg.Any<CancellationToken>()).Returns(12L);

        Assert.That(await _service.CanReadBranchAsync(7, false, true, 12), Is.True);
    }

    [Test]
    public async Task StaffCannotReadAnotherBranch()
    {
        _repository.GetStaffBranchIdAsync(7, Arg.Any<CancellationToken>()).Returns(12L);

        Assert.That(await _service.CanReadBranchAsync(7, false, true, 13), Is.False);
    }

    [Test]
    public async Task CustomerCannotReadBranch()
    {
        Assert.That(await _service.CanReadBranchAsync(7, false, false, 12), Is.False);
        await _repository.DidNotReceive().GetStaffBranchIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }
}
