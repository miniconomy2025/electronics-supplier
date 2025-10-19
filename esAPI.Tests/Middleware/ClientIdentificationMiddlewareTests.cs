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

        // Act
        var response = await client.GetAsync("/api/electronics");

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

        // Act
        var response = await client.GetAsync("/api/electronics");

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
            builder.ConfigureServices(services =>
            {
                // Remove the real database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Add in-memory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Seed test data
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                context.Companies.Add(new Company 
                { 
                    CompanyId = 1, 
                    CompanyName = "test-client",
                    BankAccountNumber = "123456"
                });
                context.SaveChanges();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("Client-Id", "test-client");

        // Act
        var response = await client.GetAsync("/api/electronics");

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

        // Act
        var response = await client.PostAsJsonAsync("/payments", new { });

        // Assert
        // Should not return 401 unauthorized since payments endpoints skip the middleware
        // (They might return other errors like BadRequest, but not Unauthorized)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}