using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Core.Models.Appointment;
using BranchBooking.Application.Services;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests;

[TestFixture]
public sealed class AppointmentServiceTests
{
    private IAppointmentRepository _repository = null!;
    private AppointmentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IAppointmentRepository>();
        _service = new AppointmentService(_repository);
    }

    [Test]
    public async Task GetAvailableSlotsAsync_ForwardsCustomerIdToRepository()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var start = date.ToDateTime(new TimeOnly(10, 0));
        var end = start.AddMinutes(30);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _repository.GetAvailableSlotsAsync(42, 7, 3, date, null, cancellationToken)
            .Returns(new[] { new AvailableSlotRecord(start, end) });

        var result = (await _service.GetAvailableSlotsAsync(42, 7, 3, date, null, cancellationToken)).ToArray();

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0].StartAt, Is.EqualTo(start));
        await _repository.Received(1).GetAvailableSlotsAsync(42, 7, 3, date, null, cancellationToken);
    }

    [Test]
    public void GetAvailableSlotsAsync_WhenCustomerIdIsInvalid_RejectsBeforeRepositoryCall()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var exception = Assert.ThrowsAsync<BranchBooking.Core.Exceptions.DomainValidationException>(
            async () => await _service.GetAvailableSlotsAsync(0, 7, 3, date));

        Assert.That(exception!.Message, Is.EqualTo("Customer ID must be greater than zero."));
        _repository.DidNotReceive().GetAvailableSlotsAsync(
            Arg.Any<long>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<long?>(), Arg.Any<CancellationToken>());
    }
}
