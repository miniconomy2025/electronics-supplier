using esAPI.DTOs;
using esAPI.Clients;
using esAPI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using WireMock.Server;
using System.Text.Json;

namespace esAPI.Tests.Integration
{
    public class BulkLogisticsIntegrationTests : IDisposable
    {
        private readonly WireMockServer _mockServer;
        private readonly IBulkLogisticsClient _bulkLogisticsClient;

        public BulkLogisticsIntegrationTests()
        {
            // Start WireMock server on a random port
            _mockServer = WireMockServer.Start();
            
            var services = new ServiceCollection();
            
            services.AddHttpClient("bulk-logistics", client =>
            {
                client.BaseAddress = new Uri(_mockServer.Url);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddScoped<IBulkLogisticsClient, BulkLogisticsClient>();
            
            var serviceProvider = services.BuildServiceProvider();
            _bulkLogisticsClient = serviceProvider.GetRequiredService<IBulkLogisticsClient>();
        }

        [Fact]
        public async Task ArrangePickupAsync_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var expectedResponse = new LogisticsPickupResponse
            {
                PickupRequestId = 12345,
                PaymentReferenceId = "PAY-REF-123",
                Status = "pending",
                StatusCheckUrl = $"{_mockServer.Url}/api/pickup-request/12345/status"
            };

            _mockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath("/api/pickup-request")
                    .UsingPost())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(201)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(expectedResponse)));

            var request = new LogisticsPickupRequest
            {
                OriginalExternalOrderId = "TEST-ORDER-123",
                OriginCompanyId = "thoh",
                DestinationCompanyId = "electronics-supplier",
                Items = new[]
                {
                    new LogisticsItem
                    {
                        Name = "electronics_machine",
                        Quantity = 1
                    }
                }
            };

            // Act
            var response = await _bulkLogisticsClient.ArrangePickupAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(12345, response.PickupRequestId);
            Assert.Equal("PAY-REF-123", response.PaymentReferenceId);
            Assert.Equal("pending", response.Status);
        }

        [Fact]
        public async Task GetPickupRequestAsync_WithValidId_ReturnsPickupDetails()
        {
            // Arrange
            var pickupId = 12345;
            var expectedResponse = new LogisticsPickupDetailsResponse
            {
                PickupRequestId = pickupId,
                OriginalExternalOrderId = "TEST-ORDER-123",
                OriginCompanyName = "thoh",
                Status = "completed",
                Items = new[]
                {
                    new LogisticsItem
                    {
                        Name = "electronics_machine",
                        Quantity = 1
                    }
                }
            };

            _mockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath($"/api/pickup-request/{pickupId}")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(expectedResponse)));

            // Act
            var response = await _bulkLogisticsClient.GetPickupRequestAsync(pickupId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(pickupId, response.PickupRequestId);
            Assert.Equal("TEST-ORDER-123", response.OriginalExternalOrderId);
            Assert.Equal("completed", response.Status);
        }

        [Fact]
        public async Task GetPickupRequestAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidPickupId = 99999;

            _mockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath($"/api/pickup-request/{invalidPickupId}")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(404));

            // Act
            var response = await _bulkLogisticsClient.GetPickupRequestAsync(invalidPickupId);

            // Assert
            Assert.Null(response);
        }

        [Fact]
        public async Task GetCompanyPickupRequestsAsync_WithValidCompany_ReturnsPickupList()
        {
            // Arrange
            var companyName = "electronics-supplier";
            var expectedResponse = new[]
            {
                new LogisticsPickupDetailsResponse
                {
                    PickupRequestId = 12345,
                    OriginalExternalOrderId = "TEST-ORDER-123",
                    OriginCompanyName = "thoh",
                    Status = "pending",
                    Items = new[]
                    {
                        new LogisticsItem
                        {
                            Name = "electronics_machine",
                            Quantity = 1
                        }
                    }
                },
                new LogisticsPickupDetailsResponse
                {
                    PickupRequestId = 12346,
                    OriginalExternalOrderId = "TEST-ORDER-124",
                    OriginCompanyName = "thoh",
                    Status = "completed",
                    Items = new[]
                    {
                        new LogisticsItem
                        {
                            Name = "electronics_machine",
                            Quantity = 2
                        }
                    }
                }
            };

            _mockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath($"/api/pickup-request/company/{companyName}")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(expectedResponse)));

            // Act
            var response = await _bulkLogisticsClient.GetCompanyPickupRequestsAsync(companyName);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.Count);
            Assert.Equal(12345, response[0].PickupRequestId);
            Assert.Equal(12346, response[1].PickupRequestId);
        }

        [Fact]
        public async Task ArrangePickupAsync_WithServerError_ThrowsException()
        {
            // Arrange
            _mockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath("/api/pickup-request")
                    .UsingPost())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(500)
                    .WithBody("Internal Server Error"));

            var request = new LogisticsPickupRequest
            {
                OriginalExternalOrderId = "TEST-ORDER-123",
                OriginCompanyId = "thoh",
                DestinationCompanyId = "electronics-supplier",
                Items = new[]
                {
                    new LogisticsItem
                    {
                        Name = "electronics_machine",
                        Quantity = 1
                    }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _bulkLogisticsClient.ArrangePickupAsync(request));
        }

        public void Dispose()
        {
            _mockServer?.Stop();
            _mockServer?.Dispose();
        }
    }
}
