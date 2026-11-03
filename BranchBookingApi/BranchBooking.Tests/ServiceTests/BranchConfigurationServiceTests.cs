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
}
