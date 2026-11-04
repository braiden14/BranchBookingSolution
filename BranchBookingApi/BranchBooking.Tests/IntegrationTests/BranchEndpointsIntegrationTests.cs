using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using BranchBooking.Application.DTOs;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Core.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace BranchBooking.Tests.IntegrationTests;

[TestFixture]
public sealed class BranchEndpointsIntegrationTests
{
    private IBranchService _branchService = null!;
    private BranchApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _branchService = Substitute.For<IBranchService>();
        _factory = new BranchApiFactory(_branchService);
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetBranches_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/branches");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        await _branchService.DidNotReceiveWithAnyArgs().GetPageAsync(default, default, default, default);
    }

    [Test]
    public async Task GetBranches_WhenAuthenticatedAsNonAdmin_ReturnsForbidden()
    {
        AddRoleHeader("Staff");

        var response = await _client.GetAsync("/api/branches");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        await _branchService.DidNotReceiveWithAnyArgs().GetPageAsync(default, default, default, default);
    }

    [Test]
    public async Task GetBranches_UsesDefaultPagingAndReturnsServiceResponse()
    {
        _branchService.GetPageAsync(1, 5, Arg.Any<CancellationToken>(), null)
            .Returns(new PagedBranchDto(Array.Empty<BranchDto>(), 1, 5, 0, 0));
        AddRoleHeader("Admin");

        var response = await _client.GetAsync("/api/branches");
        var result = await response.Content.ReadFromJsonAsync<PagedBranchDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PageSize, Is.EqualTo(5));
        await _branchService.Received(1).GetPageAsync(1, 5, Arg.Any<CancellationToken>(), null);
    }

    [Test]
    public async Task GetBranches_ForwardsPagingAndSearchQuery()
    {
        _branchService.GetPageAsync(3, 10, Arg.Any<CancellationToken>(), "Johannesburg")
            .Returns(new PagedBranchDto(Array.Empty<BranchDto>(), 3, 10, 0, 0));
        AddRoleHeader("Admin");

        var response = await _client.GetAsync("/api/branches?pageNumber=3&pageSize=10&searchTerm=Johannesburg");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await _branchService.Received(1).GetPageAsync(3, 10, Arg.Any<CancellationToken>(), "Johannesburg");
    }

    private void AddRoleHeader(string role)
    {
        _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
    }
}

sealed class BranchApiFactory(IBranchService branchService) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:BookingDatabase"] = "Host=localhost;Database=integration-tests;Username=test;Password=test",
            ["Jwt:Issuer"] = "integration-tests",
            ["Jwt:Audience"] = "integration-tests",
            ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-characters"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBranchService>();
            services.AddSingleton(branchService);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
        });
    }
}

sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTest";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
