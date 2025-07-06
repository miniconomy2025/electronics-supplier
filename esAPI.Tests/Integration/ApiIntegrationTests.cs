using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using esAPI.Data;
using esAPI.Models;
using esAPI.DTOs.Electronics;

namespace esAPI.Tests.Integration
{
    public class ElectronicsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ElectronicsEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add in-memory database
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("IntegrationTestDb"));
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetElectronics_WithRealDatabase_ReturnsCorrectResponse()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Seed test data
            await SeedTestData(context);

            // Act
            var response = await _client.GetAsync("/electronics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var jsonString = await response.Content.ReadAsStringAsync();
            var electronicsDetails = JsonSerializer.Deserialize<ElectronicsDetailsDto>(
                jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            electronicsDetails.Should().NotBeNull();
            electronicsDetails!.AvailableStock.Should().BeGreaterOrEqualTo(0);
            electronicsDetails.PricePerUnit.Should().BeGreaterThan(0);
        }

        private async Task SeedTestData(AppDbContext context)
        {
            // Clear existing data
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Add basic seed data similar to your migration
            var simulation = new Simulation
            {
                DayNumber = 1,
                StartedAt = DateTime.UtcNow,
                IsRunning = true
            };
            context.Simulations.Add(simulation);

            var lookupValue = new LookupValue
            {
                ElectronicsPricePerUnit = 25.50m,
                ChangedAt = 1.0m
            };
            context.LookupValues.Add(lookupValue);

            var electronicsStatus = new ElectronicsStatus
            {
                StatusId = 1,
                Status = "AVAILABLE"
            };
            context.ElectronicsStatuses.Add(electronicsStatus);

            // Add some electronics
            var electronics = new List<Electronic>
            {
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = 1 },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = 1 },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = 1 }
            };
            context.Electronics.AddRange(electronics);

            await context.SaveChangesAsync();
        }
    }
}
