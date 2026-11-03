using BranchBooking.Application.Interfaces.Repositories;
using BranchBooking.Application.Services;
using BranchBooking.Core.Models.Customer;
using NSubstitute;

namespace BranchBooking.Tests.ServiceTests;

[TestFixture]
public sealed class CustomerServiceTests
{
    private ICustomerRepository _repository = null!;
    private CustomerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ICustomerRepository>();
        _service = new CustomerService(_repository);
    }

    [Test]
    public async Task GetPageAsync_ClampsPagingAndMapsCustomer()
    {
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        _repository.GetPageAsync(1, 200, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new CustomerRecord(7, "Jane", "Doe", "jane@example.com", "+27123456789", 1, createdAt, 201)
        });

        var result = await _service.GetPageAsync(0, 999);

        Assert.That(result.PageNumber, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(200));
        Assert.That(result.TotalCount, Is.EqualTo(201));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].CustomerId, Is.EqualTo(7));
            Assert.That(result.Items[0].Email, Is.EqualTo("jane@example.com"));
            Assert.That(result.Items[0].CreatedAtUtc, Is.EqualTo(createdAt));
        });
        await _repository.Received(1).GetPageAsync(1, 200, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetPageAsync_WhenNoCustomers_ReturnsZeroTotals()
    {
        _repository.GetPageAsync(2, 20, Arg.Any<CancellationToken>()).Returns(Array.Empty<CustomerRecord>());

        var result = await _service.GetPageAsync(2, 20);

        Assert.That(result.Items, Is.Empty);
        Assert.That(result.TotalCount, Is.Zero);
        Assert.That(result.TotalPages, Is.Zero);
    }
}
