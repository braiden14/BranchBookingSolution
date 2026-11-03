using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Models.Schedule;
using BranchBooking.Application.Services;
using BranchBooking.Core.Exceptions;
using BranchBooking.Core.Models.Schedule;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests;

[TestFixture]
public sealed class OperationalScheduleServiceTests
{
    private IOperationalScheduleRepository _repository = null!;
    private OperationalScheduleService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IOperationalScheduleRepository>();
        _service = new OperationalScheduleService(_repository);
    }

    [Test]
    public async Task CreateAsync_WhenRequestIsValid_ForwardsToRepository()
    {
        var request = new CreateOperationalScheduleRequest(
            new DateOnly(2025, 1, 1),
            null,
            CreateWeeklyHours());
        _repository.CreateAsync(7, request, Arg.Any<CancellationToken>()).Returns(42L);

        var result = await _service.CreateAsync(7, request);

        Assert.That(result, Is.EqualTo(42L));
        await _repository.Received(1).CreateAsync(7, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public void CreateAsync_WhenWeeklyHoursDoNotContainSevenDays_Throws()
    {
        var request = new CreateOperationalScheduleRequest(
            new DateOnly(2025, 1, 1),
            null,
            CreateWeeklyHours().Take(6).ToArray());

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () => await _service.CreateAsync(7, request));

        Assert.That(exception!.Message, Is.EqualTo("Exactly seven weekly operating-hours entries are required."));
        _repository.DidNotReceive().CreateAsync(Arg.Any<long>(), Arg.Any<CreateOperationalScheduleRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void CreateAsync_WhenDaysAreDuplicated_Throws()
    {
        var hours = CreateWeeklyHours();
        hours[6] = hours[0] with { DayOfWeek = 1 };

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () =>
            await _service.CreateAsync(7, new CreateOperationalScheduleRequest(new DateOnly(2025, 1, 1), null, hours)));

        Assert.That(exception!.Message, Is.EqualTo("Weekly operating hours must contain one entry for each day from Monday to Sunday."));
    }

    [Test]
    public void CreateAsync_WhenOpenDayHasNoCapacity_Throws()
    {
        var hours = CreateWeeklyHours();
        hours[0] = hours[0] with { Capacity = 0 };

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () =>
            await _service.CreateAsync(7, new CreateOperationalScheduleRequest(new DateOnly(2025, 1, 1), null, hours)));

        Assert.That(exception!.Message, Is.EqualTo("Open days must have a capacity greater than zero."));
    }

    [Test]
    public void CreateAsync_WhenOpenDayHasInvalidTimes_Throws()
    {
        var hours = CreateWeeklyHours();
        hours[0] = hours[0] with { OpensAt = new TimeOnly(12), ClosesAt = new TimeOnly(10) };

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () =>
            await _service.CreateAsync(7, new CreateOperationalScheduleRequest(new DateOnly(2025, 1, 1), null, hours)));

        Assert.That(exception!.Message, Is.EqualTo("Closing time must be after opening time."));
    }

    [Test]
    public async Task CreateAsync_WhenDayIsClosed_AllowsMissingTimes()
    {
        var hours = CreateWeeklyHours();
        hours[0] = new WeeklyOperatingHoursRequest(1, null, null, true, null);
        _repository.CreateAsync(7, Arg.Any<CreateOperationalScheduleRequest>(), Arg.Any<CancellationToken>()).Returns(42L);

        var result = await _service.CreateAsync(7, new CreateOperationalScheduleRequest(new DateOnly(2025, 1, 1), null, hours));

        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public async Task GetForBranchAsync_MapsScheduleAndReturnsNullWhenMissing()
    {
        var weeklyHours = new[] { new WeeklyOperatingHoursRecord(1, 2, 1, new TimeOnly(9), new TimeOnly(17), false, 10) };
        _repository.GetForBranchAsync(7, new DateOnly(2025, 1, 1), Arg.Any<CancellationToken>())
            .Returns(new OperationalScheduleRecord(2, 7, new DateOnly(2025, 1, 1), null, true, weeklyHours));

        var result = await _service.GetForBranchAsync(7, new DateOnly(2025, 1, 1));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BranchId, Is.EqualTo(7));
        Assert.That(result.WeeklyHours, Is.EqualTo(weeklyHours));
    }

    [Test]
    public void GetForBranchAsync_WhenBranchIdIsInvalid_Throws()
    {
        var exception = Assert.ThrowsAsync<DomainValidationException>(async () =>
            await _service.GetForBranchAsync(0, new DateOnly(2025, 1, 1)));

        Assert.That(exception!.Message, Is.EqualTo("Branch ID must be greater than zero."));
        _repository.DidNotReceive().GetForBranchAsync(Arg.Any<long>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    private static List<WeeklyOperatingHoursRequest> CreateWeeklyHours() => Enumerable.Range(1, 7)
        .Select(day => new WeeklyOperatingHoursRequest((short)day, new TimeOnly(9), new TimeOnly(17), false, 10))
        .ToList();
}
