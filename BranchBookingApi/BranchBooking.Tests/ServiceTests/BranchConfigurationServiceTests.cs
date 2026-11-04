using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Core.Models.Configuration;
using BranchBooking.Application.Models.Request;
using BranchBooking.Application.Services;
using BranchBooking.Core.Exceptions;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests;

[TestFixture]
public sealed class BranchConfigurationServiceTests
{
    private IBranchConfigurationRepository _configurationRepository = null!;
    private IOperationalScheduleRepository _scheduleRepository = null!;
    private BranchConfigurationService _configurationService = null!;

    [SetUp]
    public void SetUp()
    {
        _configurationRepository = Substitute.For<IBranchConfigurationRepository>();
        _scheduleRepository = Substitute.For<IOperationalScheduleRepository>();
        _configurationService = new BranchConfigurationService(_configurationRepository, _scheduleRepository);
    }

    [Test]
    public void DeleteOfferedServiceAsync_WhenServiceDoesNotExist_RejectsRequest()
    {
        _configurationRepository.DeleteOfferedServiceAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var exception = Assert.ThrowsAsync<DomainValidationException>(
            async () => await _configurationService.DeleteOfferedServiceAsync(99));

        Assert.That(exception!.Message, Is.EqualTo("The offered service could not be found."));
    }

    [Test]
    public async Task GetBranchServicesAsync_MapsRepositoryRecordsToDtos()
    {
        // Given
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        _configurationRepository
            .GetBranchServicesAsync(7, cancellationToken)
            .Returns(new[]
            {
                new BranchServiceRecord(11, 7, 3, "CONSULT", "Consultation", 30, true, createdAt)
            });

        // When
        var result = (await _configurationService.GetBranchServicesAsync(7, cancellationToken)).ToArray();

        // Then
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].BranchServiceId, Is.EqualTo(11));
            Assert.That(result[0].BranchId, Is.EqualTo(7));
            Assert.That(result[0].ServiceCode, Is.EqualTo("CONSULT"));
            Assert.That(result[0].IsActive, Is.True);
        });
        await _configurationRepository.Received(1).GetBranchServicesAsync(7, cancellationToken);
    }

    [Test]
    public void UpsertBranchServiceAsync_WhenServiceIdIsInvalid_RejectsRequest()
    {
        var exception = Assert.ThrowsAsync<DomainValidationException>(
            async () => await _configurationService.UpsertBranchServiceAsync(7, new UpsertBranchServiceRequest(0, true)));

        Assert.That(exception!.Message, Is.EqualTo("A valid service is required."));
        _configurationRepository.DidNotReceive().UpsertBranchServiceAsync(
            Arg.Any<long>(), Arg.Any<UpsertBranchServiceRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void UpsertCapacityOverrideAsync_WhenCapacityIsNegative_RejectsRequest()
    {
        var request = new UpsertCapacityOverrideRequest(new DateOnly(2025, 1, 2), -1);

        var exception = Assert.ThrowsAsync<DomainValidationException>(
            async () => await _configurationService.UpsertCapacityOverrideAsync(7, request));

        Assert.That(exception!.Message, Is.EqualTo("Capacity cannot be negative."));
        _configurationRepository.DidNotReceive().UpsertCapacityOverrideAsync(
            Arg.Any<long>(), Arg.Any<UpsertCapacityOverrideRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateDateOverrideAsync_WhenScheduleDoesNotBelongToBranch_RejectsRequest()
    {
        var date = new DateOnly(2025, 1, 2);
        _scheduleRepository.GetForBranchAsync(7, date, Arg.Any<CancellationToken>()).Returns((Core.Models.Schedule.OperationalScheduleRecord?)null);
        var request = new CreateDateOverrideRequest(99, date, null, false, new TimeOnly(9), new TimeOnly(17), 10);

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () => await _configurationService.CreateDateOverrideAsync(7, request));

        Assert.That(exception!.Message, Is.EqualTo("The operational schedule does not belong to this branch or date."));
        await _configurationRepository.DidNotReceive().CreateDateOverrideAsync(Arg.Any<long>(), Arg.Any<CreateDateOverrideRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateDateOverrideAsync_WhenOverrideDoesNotBelongToBranch_RejectsRequest()
    {
        var date = new DateOnly(2025, 1, 2);
        _configurationRepository.GetDateOverridesAsync(7, null, null, Arg.Any<CancellationToken>()).Returns(Array.Empty<DateOverrideRecord>());
        var request = new UpdateDateOverrideRequest(date, null, false, new TimeOnly(9), new TimeOnly(17), 10);

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () => await _configurationService.UpdateDateOverrideAsync(7, 99, request));

        Assert.That(exception!.Message, Is.EqualTo("Date override was not found for this branch."));
        await _configurationRepository.DidNotReceive().UpdateDateOverrideAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<UpdateDateOverrideRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetDateOverridesAsync_WhenDateRangeIsInvalid_RejectsBeforeRepositoryCall()
    {
        var from = new DateOnly(2025, 1, 10);
        var to = new DateOnly(2025, 1, 1);

        var exception = Assert.ThrowsAsync<DomainValidationException>(async () => await _configurationService.GetDateOverridesAsync(7, from, to));

        Assert.That(exception!.Message, Is.EqualTo("The end date cannot be before the start date."));
        await _configurationRepository.DidNotReceive().GetDateOverridesAsync(Arg.Any<long>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>());
    }
}
