using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using esAPI.Data;
using esAPI.Models;
using esAPI.Clients;
using esAPI.Interfaces;
using esAPI.Services;
using Moq;
using Xunit;

namespace esAPI.Tests.Middleware;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDb_" + Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Remove problematic services that we don't need for middleware testing
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType.Name.Contains("DbContext") ||
                d.ImplementationType?.FullName?.Contains("Npgsql") == true ||
                d.ServiceType.FullName?.Contains("Npgsql") == true ||
                d.ServiceType.FullName?.Contains("PostgreSQL") == true ||
                d.ServiceType == typeof(RetryQueuePublisher) ||
                d.ImplementationType == typeof(RetryQueuePublisher) ||
                // Remove hosted services that cause issues in tests
                d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) ||
                d.ImplementationType?.Name.Contains("AutoAdvanceService") == true ||
                d.ImplementationType?.Name.Contains("BackgroundService") == true ||
                d.ImplementationType?.Name.Contains("RetryJobDispatcher") == true)
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // Add In-Memory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            }, ServiceLifetime.Scoped);

            // Add mock external API clients
            var mockCommercialBankClient = new Mock<ICommercialBankClient>();
            mockCommercialBankClient.Setup(x => x.GetAccountBalanceAsync()).ReturnsAsync(1000000m);
            mockCommercialBankClient.Setup(x => x.GetAccountDetailsAsync()).ReturnsAsync("TEST-ACCOUNT");
            services.AddSingleton(mockCommercialBankClient.Object);

            var mockBulkLogisticsClient = new Mock<IBulkLogisticsClient>();
            services.AddSingleton(mockBulkLogisticsClient.Object);

            var mockThohApiClient = new Mock<IThohApiClient>();
            services.AddSingleton(mockThohApiClient.Object);

            var mockRecyclerApiClient = new Mock<IRecyclerApiClient>();
            services.AddSingleton(mockRecyclerApiClient.Object);
        });
    }

    public void SeedTestData()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        if (!context.Companies.Any(c => c.CompanyName == "test-client"))
        {
            context.Companies.Add(new Company { CompanyName = "test-client" });
            context.SaveChanges();
        }
    }
}

public class ClientIdentificationMiddlewareTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ClientIdentificationMiddlewareTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // This method is no longer needed since we have the TestWebApplicationFactory

    [Fact]
    public async Task Request_WithoutClientIdHeader_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Use a simple POST endpoint that should trigger middleware but won't cause validation issues
        var response = await client.PostAsync("/electronics", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Missing or empty Client-Id header", content);
    }

    [Fact]
    public async Task Request_WithUnknownClientId_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Client-Id", "unknown-client");

        // Act - Use a simple POST endpoint that should trigger middleware but won't cause validation issues  
        var response = await client.PostAsync("/electronics", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unknown client: unknown-client", content);
    }

    [Fact]
    public async Task Request_WithValidClientId_PassesMiddleware()
    {
        // Arrange
        _factory.SeedTestData(); // Seed the test client
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Client-Id", "test-client");

        // Act - Use a simple POST endpoint that should trigger middleware but won't cause validation issues
        var response = await client.PostAsync("/electronics", null);

        // Assert
        // The request should pass the middleware (not get 401)
        // It might still return other status codes based on the actual endpoint logic
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_SkipsMiddleware()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/index.html");

        // Assert
        // Should not return 401 unauthorized since swagger endpoints skip the middleware
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PaymentsEndpoint_SkipsMiddleware()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Send a valid payment request to avoid validation errors
        var response = await client.PostAsJsonAsync("/payments", new
        {
            TransactionNumber = "TXN123",
            Status = "COMPLETED",
            Amount = 100.00m,
            Timestamp = 1642678800.0,
            Description = "Test payment",
            From = "test-account",
            To = "supplier-account"
        });

        // Assert
        // Should not return 401 unauthorized since payments endpoints skip the middleware
        // (They might return other errors like BadRequest, but not Unauthorized)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
