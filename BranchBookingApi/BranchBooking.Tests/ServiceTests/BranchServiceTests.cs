using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Core.Models.Branch;
using BranchBooking.Application.Models.Request;
using BranchBooking.Application.Services;
using BranchBooking.Core.Enums;
using BranchBooking.Core.Exceptions;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests
{
    [TestFixture]
    public class BranchServiceTests
    {
        private IBranchRepository _branchRepository = null!;
        private BranchService _branchService = null!;

        [SetUp]
        public void SetUp()
        {
            _branchRepository = Substitute.For<IBranchRepository>();
            _branchService = new BranchService(_branchRepository);
        }

        [Test]
        public async Task GetAllAsync_WhenBranchesExist_ReturnsMappedBranchDtos()
        {
            // Given
            // Arrange
            const int pageNumber = 2;
            const int pageSize = 10;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var branch = new Branch(
                "JHB01",
                " Johannesburg Branch ".Trim(),
                "1 Main Street",
                "Johannesburg",
                "Gauteng",
                "2000",
                BranchStatus.Active,
                true);
            _branchRepository
                .GetAllAsync(pageNumber, pageSize, cancellationToken)
                .Returns(new[] { branch });

            // When
            // Act
            var result = (await _branchService.GetAllAsync(pageNumber, pageSize, cancellationToken)).ToArray();

            // Then
            // Assert
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result[0].BranchId, Is.EqualTo(branch.Id));
                Assert.That(result[0].Code, Is.EqualTo(branch.Code));
                Assert.That(result[0].Name, Is.EqualTo(branch.Name));
                Assert.That(result[0].AddressLine1, Is.EqualTo(branch.AddressLine1));
                Assert.That(result[0].City, Is.EqualTo(branch.City));
                Assert.That(result[0].Province, Is.EqualTo(branch.Province));
                Assert.That(result[0].PostalCode, Is.EqualTo(branch.PostalCode));
                Assert.That(result[0].Status, Is.EqualTo(branch.Status));
                Assert.That(result[0].AcceptsBookings, Is.EqualTo(branch.AcceptsBookings));
                Assert.That(result[0].CreatedAtUtc, Is.EqualTo(branch.CreatedAtUtc));
            });
            await _branchRepository.Received(1).GetAllAsync(pageNumber, pageSize, cancellationToken);
        }

        [Test]
        public async Task GetPageAsync_MapsListRecordsAndClampsPagingValues()
        {
            // Given
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var createdAt = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
            _branchRepository
                .GetPageAsync(1, 200, cancellationToken)
                .Returns(new[]
                {
                    new BranchListItemRecord(7, "JHB01", "Johannesburg", "1 Main Street", "Johannesburg", "Gauteng", "2000", 1, true, createdAt, 201)
                });

            // When
            var result = await _branchService.GetPageAsync(0, 999, cancellationToken);

            // Then
            Assert.That(result.PageNumber, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(200));
            Assert.That(result.TotalCount, Is.EqualTo(201));
            Assert.That(result.TotalPages, Is.EqualTo(2));
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(result.Items[0].BranchId, Is.EqualTo(7));
                Assert.That(result.Items[0].Status, Is.EqualTo(BranchStatus.Active));
                Assert.That(result.Items[0].CreatedAtUtc, Is.EqualTo(createdAt));
            });
            await _branchRepository.Received(1).GetPageAsync(1, 200, cancellationToken);
        }

        [Test]
        public async Task UpdateAsync_WhenRequestIsValid_ForwardsRequestToRepository()
        {
            // Given
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var request = new UpdateBranchRequest("JHB02", "Rosebank", "10 Main Road", "Johannesburg", "Gauteng", "2196");
            _branchRepository.UpdateAsync(7, request, cancellationToken).Returns(true);

            // When
            await _branchService.UpdateAsync(7, request, cancellationToken);

            // Then
            await _branchRepository.Received(1).UpdateAsync(7, request, cancellationToken);
        }

        [Test]
        public void UpdateAsync_WhenBranchIdIsInvalid_ThrowsBeforeCallingRepository()
        {
            var request = new UpdateBranchRequest("JHB02", "Rosebank", "10 Main Road", "Johannesburg", "Gauteng", "2196");

            var exception = Assert.ThrowsAsync<DomainValidationException>(
                async () => await _branchService.UpdateAsync(0, request));

            Assert.That(exception!.Message, Is.EqualTo("Branch ID must be greater than zero."));
            _branchRepository.DidNotReceive().UpdateAsync(Arg.Any<long>(), Arg.Any<UpdateBranchRequest>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task AddAsync_WhenRequestIsValid_TrimsValuesAndAddsInactiveBranch()
        {
            // Given
            // Arrange
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var request = new CreateBranchRequest(
                "CPT01",
                " Cape Town Branch ",
                " 1 Main Street ",
                " Cape Town ",
                " Western Cape ",
                " 8000 ");
            Branch? addedBranch = null;
            _branchRepository
                .AddAsync(Arg.Do<Branch>(branch => addedBranch = branch), cancellationToken)
                .Returns(42L);

            // When
            // Act
            var result = await _branchService.AddAsync(request, cancellationToken);

            // Then
            // Assert
            Assert.That(result, Is.EqualTo(42L));
            Assert.That(addedBranch, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(addedBranch!.Code, Is.EqualTo(request.Code));
                Assert.That(addedBranch.Name, Is.EqualTo("Cape Town Branch"));
                Assert.That(addedBranch.AddressLine1, Is.EqualTo("1 Main Street"));
                Assert.That(addedBranch.City, Is.EqualTo("Cape Town"));
                Assert.That(addedBranch.Province, Is.EqualTo("Western Cape"));
                Assert.That(addedBranch.PostalCode, Is.EqualTo("8000"));
                Assert.That(addedBranch.Status, Is.EqualTo(BranchStatus.Inactive));
                Assert.That(addedBranch.AcceptsBookings, Is.False);
            });
            await _branchRepository.Received(1).AddAsync(Arg.Any<Branch>(), cancellationToken);
        }

        [Test]
        public void AddAsync_WhenRequestIsNull_ThrowsDomainValidationException()
        {
            // Given
            // Arrange

            // When
            // Act
            var exception = Assert.ThrowsAsync<DomainValidationException>(
                async () => await _branchService.AddAsync(null!));

            // Then
            // Assert
            Assert.That(exception!.Message, Is.EqualTo("Branch request is required."));
            _branchRepository.DidNotReceive().AddAsync(Arg.Any<Branch>(), Arg.Any<CancellationToken>());
        }
    }
}
