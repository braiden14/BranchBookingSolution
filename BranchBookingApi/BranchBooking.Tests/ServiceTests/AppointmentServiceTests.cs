using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Request;
using BranchBooking.Core.Models.Appointment;
using BranchBooking.Application.Services;
using BranchBooking.Core.Exceptions;
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

    [Test]
    public async Task CreateAsync_WhenSlotIsAvailable_CreatesAppointment()
    {
        var start = DateTime.SpecifyKind(DateTime.Now.AddDays(1).Date.AddHours(10), DateTimeKind.Unspecified);
        _repository.GetAvailableSlotsAsync(42, 7, 3, DateOnly.FromDateTime(start), null, Arg.Any<CancellationToken>())
            .Returns(new[] { new AvailableSlotRecord(start, start.AddMinutes(30)) });
        _repository.CreateAsync(42, 7, 3, start, Arg.Any<CancellationToken>()).Returns(55L);

        var result = await _service.CreateAsync(42, new CreateAppointmentRequest(7, 3, start));

        Assert.That(result, Is.EqualTo(55L));
        await _repository.Received(1).CreateAsync(42, 7, 3, start, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WhenSlotIsUnavailable_Throws()
    {
        var start = DateTime.SpecifyKind(DateTime.Now.AddDays(1).Date.AddHours(10), DateTimeKind.Unspecified);
        _repository.GetAvailableSlotsAsync(42, 7, 3, DateOnly.FromDateTime(start), null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AvailableSlotRecord>());

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () =>
            await _service.CreateAsync(42, new CreateAppointmentRequest(7, 3, start)));

        Assert.That(exception!.Message, Is.EqualTo("The selected appointment slot is no longer available."));
        await _repository.DidNotReceive().CreateAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WhenStartTimeIsNotOnQuarterHour_Throws()
    {
        var start = DateTime.SpecifyKind(DateTime.Now.AddDays(1).Date.AddHours(10).AddMinutes(1), DateTimeKind.Unspecified);

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () =>
            await _service.CreateAsync(42, new CreateAppointmentRequest(7, 3, start)));

        Assert.That(exception!.Message, Is.EqualTo("Appointments must use 15-minute local SAST slots."));
        await _repository.DidNotReceive().GetAvailableSlotsAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<long?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetOperationalAppointmentsAsync_ClampsPagingAndMapsRecords()
    {
        var start = DateTime.UtcNow.AddDays(1);
        _repository.GetOperationalAppointmentsAsync(7, false, 1, 200, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new OperationalAppointmentRecord(1, 42, "Jane Doe", "jane@example.com", 7, "Main", 3, "Consultation", start, start.AddMinutes(30), Core.Enums.AppointmentStatus.Confirmed, 201)
        });

        var result = await _service.GetOperationalAppointmentsAsync(7, false, 0, 999);

        Assert.That(result.PageNumber, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(200));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.Items.Single().CustomerEmail, Is.EqualTo("jane@example.com"));
    }
}
