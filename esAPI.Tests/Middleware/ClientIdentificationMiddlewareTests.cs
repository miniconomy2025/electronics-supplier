using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using esAPI.Data;
using esAPI.Models;
using Xunit;

namespace esAPI.Tests.Middleware;

public class ClientIdentificationMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ClientIdentificationMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_WithoutClientIdHeader_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Use a POST endpoint that should trigger middleware (not GET since all GET requests are skipped)
        var response = await client.PostAsJsonAsync("/electronics", new { });

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

        // Act - Use a POST endpoint that should trigger middleware (not GET since all GET requests are skipped)
        var response = await client.PostAsJsonAsync("/electronics", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unknown client: unknown-client", content);
    }

    [Fact]
    public async Task Request_WithValidClientId_PassesMiddleware()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove ALL database-related services completely
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ImplementationType?.FullName?.Contains("AppDbContext") == true ||
                    d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in toRemove)
                {
                    services.Remove(descriptor);
                }

                // Replace with in-memory database
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString()),
                    ServiceLifetime.Scoped);
            });
        }).CreateClient();

        // Get the HttpClient to trigger service provider initialization, then seed database
        var testClient = client;
        
        client.DefaultRequestHeaders.Add("Client-Id", "test-client");

        // Act - Use a POST endpoint that should trigger middleware
        var response = await client.PostAsJsonAsync("/electronics", new { });

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