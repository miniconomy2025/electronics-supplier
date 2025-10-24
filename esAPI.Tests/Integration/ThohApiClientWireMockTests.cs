using System.Text.Json;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using esAPI.Clients;
using esAPI.DTOs;

namespace esAPI.Tests.Integration
{
    public class ThohApiClientWireMockTests : IDisposable
    {
        private readonly WireMockServer _server;

        public ThohApiClientWireMockTests()
        {
            // Start a WireMock server on an available port
            _server = WireMockServer.Start();
        }

        public void Dispose()
        {
            _server.Stop();
            _server.Dispose();
        }

        private IHttpClientFactory CreateFactoryForWireMock()
        {
            var client = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("thoh")).Returns(client);
            return factoryMock.Object;
        }

        [Fact]
        public async Task GetAvailableMaterialsAsync_UsesWireMock_ReturnsMappedDto()
        {
            // Arrange
            var body = new[]
            {
                new { rawMaterialName = "Copper", quantityAvailable = 100, pricePerKg = 50.5m }
            };

            _server.Given(Request.Create().WithPath("/api/raw-materials").UsingGet())
                   .RespondWith(Response.Create()
                       .WithHeader("Content-Type", "application/json")
                       .WithBodyAsJson(body)
                       .WithStatusCode(200));

            var factory = CreateFactoryForWireMock();
            var client = new ThohApiClient(factory);

            // Act
            var result = await client.GetAvailableMaterialsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("Copper", result[0].MaterialName);
            Assert.Equal(100, result[0].AvailableQuantity);
            Assert.Equal(50.5m, result[0].PricePerKg);
        }

        [Fact]
        public async Task PlaceOrderAsync_PostsJsonAndReturnsResponse_VerifiesRequestReceived()
        {
            // Arrange: stub POST
            var responsePayload = new { price = 123.45m, bankAccount = "123-456", orderId = 999 };
            _server.Given(Request.Create().WithPath("/api/raw-materials").UsingPost())
                   .RespondWith(Response.Create()
                       .WithHeader("Content-Type", "application/json")
                       .WithBodyAsJson(responsePayload)
                       .WithStatusCode(200));

            var factory = CreateFactoryForWireMock();
            var client = new ThohApiClient(factory);

            var req = new SupplierOrderRequest { MaterialName = "Copper", WeightQuantity = 10 };

            // Act
            var resp = await client.PlaceOrderAsync(req);

            // Assert parsing
            Assert.NotNull(resp);
            Assert.Equal(999, resp.OrderId);
            Assert.Equal(123.45m, resp.Price);

            // Verify WireMock recorded the request
            var logs = _server.FindLogEntries(Request.Create().WithPath("/api/raw-materials").UsingPost());
            Assert.NotEmpty(logs);

            var requestBody = logs[0].RequestMessage.Body;
            // Ensure body was recorded
            Assert.False(string.IsNullOrEmpty(requestBody));
            var parsed = JsonSerializer.Deserialize<SupplierOrderRequest?>(requestBody!);
            Assert.NotNull(parsed);
            Assert.Equal("Copper", parsed!.MaterialName);
            Assert.Equal(10, parsed.WeightQuantity);
        }

        [Fact]
        public async Task GetAvailableMachinesAsync_ReturnsThohMachineList()
        {
            var payload = new
            {
                machines = new[]
                {
                    new { machineName = "M1", inputs = "copper", quantity = 1, inputRatio = new { copper = 1 }, productionRate = 10, price = 1000, weight = 10.0m }
                }
            };

            _server.Given(Request.Create().WithPath("/api/machines").UsingGet())
                   .RespondWith(Response.Create()
                       .WithHeader("Content-Type", "application/json")
                       .WithBodyAsJson(payload)
                       .WithStatusCode(200));

            var factory = CreateFactoryForWireMock();
            var client = new ThohApiClient(factory);

            var machines = await client.GetAvailableMachinesAsync();

            Assert.Single(machines);
            Assert.Equal("M1", machines[0].MachineName);
            Assert.Equal(1000.0m, machines[0].Price);
        }

        [Fact]
        public async Task GetAvailableMaterialsAsync_ServerError_ReturnsEmptyList()
        {
            // Arrange: server returns 500
            _server.Given(Request.Create().WithPath("/api/raw-materials").UsingGet())
                   .RespondWith(Response.Create().WithStatusCode(500));

            var factory = CreateFactoryForWireMock();
            var client = new ThohApiClient(factory);

            // Act
            var result = await client.GetAvailableMaterialsAsync();

            // Assert: client should handle error and return empty list
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailableMaterialsAsync_MalformedJson_ReturnsEmptyList()
        {
            // Arrange: server returns non-JSON content
            _server.Given(Request.Create().WithPath("/api/raw-materials").UsingGet())
                   .RespondWith(Response.Create()
                       .WithHeader("Content-Type", "application/json")
                       .WithBody("this is not valid json")
                       .WithStatusCode(200));

            var factory = CreateFactoryForWireMock();
            var client = new ThohApiClient(factory);

            // Act
            var result = await client.GetAvailableMaterialsAsync();

            // Assert: client should handle malformed JSON and return empty list
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task PlaceOrder_PostsExpectedJsonAndContentType()
        {
            // Arrange: respond OK
            var responsePayload = new { price = 9.99m, bankAccount = "acct-1", orderId = 42 };
            _server.Given(Request.Create().WithPath("/api/raw-materials").UsingPost())
                   .RespondWith(Response.Create()
                       .WithHeader("Content-Type", "application/json")
                       .WithBodyAsJson(responsePayload)
                       .WithStatusCode(200));

            var factory = CreateFactoryForWireMock();
            var client = new ThohApiClient(factory);

            var req = new SupplierOrderRequest { MaterialName = "Copper", WeightQuantity = 5 };

            // Act
            var resp = await client.PlaceOrderAsync(req);

            // Assert response parsed
            Assert.NotNull(resp);
            Assert.Equal(42, resp.OrderId);

            // Verify request recorded and has Content-Type header + expected JSON body
            var logs = _server.FindLogEntries(Request.Create().WithPath("/api/raw-materials").UsingPost());
            Assert.NotEmpty(logs);

            var entry = logs[0].RequestMessage;
            // header check (case-insensitive)
            // Headers may be null depending on the WireMock version; null-safe check
            Assert.True(entry.Headers?.Keys.Any(k => string.Equals(k, "Content-Type", StringComparison.OrdinalIgnoreCase)) == true, "Request missing Content-Type header");

            var actualBody = entry.Body;
            var expectedBody = JsonSerializer.Serialize(req);
            Assert.Equal(expectedBody, actualBody);
        }

        [Fact]
        public async Task PlaceOrder_ReturnsNullOnBadRequest()
        {
            // Arrange: server returns 400
            _server.Given(Request.Create().WithPath("/api/raw-materials").UsingPost())
                   .RespondWith(Response.Create().WithStatusCode(400));

            var factory = CreateFactoryForWireMock();
            var client = new ThohApiClient(factory);

            var req = new SupplierOrderRequest { MaterialName = "Copper", WeightQuantity = 5 };

            // Act
            var resp = await client.PlaceOrderAsync(req);

            // Assert client surface returns null for non-success
            Assert.Null(resp);
        }
    }
}
