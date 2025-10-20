using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Xunit;
using esAPI.Data;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Models;
using esAPI.Clients;
using System.Net.Http;
using System.Net;
using System.Text;

namespace esAPI.Tests.Services
{
    public class MachineManagementServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ICommercialBankClient> _bankClientMock;
        private readonly Mock<ILogger<MachineManagementService>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly MachineManagementService _service;

        public MachineManagementServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _bankClientMock = new Mock<ICommercialBankClient>();
            _loggerMock = new Mock<ILogger<MachineManagementService>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Setup HttpClient mock
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://thoh-api.example.com")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("thoh")).Returns(httpClient);

            _service = new MachineManagementService(
                _context,
                _httpClientFactoryMock.Object,
                _bankClientMock.Object,
                _loggerMock.Object);

            SetupTestData();
        }

        private void SetupTestData()
        {
            // Add machine statuses
            _context.MachineStatuses.AddRange(
                new MachineStatus { StatusId = 1, Status = "Standby" },
                new MachineStatus { StatusId = 2, Status = "InUse" },
                new MachineStatus { StatusId = 3, Status = "Broken" }
            );

            _context.SaveChanges();
        }

        [Fact]
        public async Task EnsureMachinesAvailableAsync_WithWorkingMachines_ShouldReturnTrue()
        {
            // Arrange
            _context.Machines.AddRange(
                new Machine { MachineId = 1, MachineStatusId = 1, RemovedAt = null }, // Standby
                new Machine { MachineId = 2, MachineStatusId = 2, RemovedAt = null }  // InUse
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.EnsureMachinesAvailableAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task EnsureMachinesAvailableAsync_WithNoMachines_ShouldPurchaseNew()
        {
            // Arrange - No machines in database
            var thohResponse = """
                {
                    "orderId": 123,
                    "totalPrice": 20000,
                    "bankAccount": "thoh-bank-123"
                }
                """;

            SetupHttpResponse(HttpStatusCode.OK, thohResponse);
            _bankClientMock.Setup(x => x.MakePaymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync("payment-123");

            // Act
            var result = await _service.EnsureMachinesAvailableAsync();

            // Assert
            Assert.True(result);
            _bankClientMock.Verify(x => x.MakePaymentAsync("thoh-bank-123", "thoh", 20000, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task EnsureMachinesAvailableAsync_WithAllBrokenMachines_ShouldPurchaseNew()
        {
            // Arrange - All machines are broken
            _context.Machines.AddRange(
                new Machine { MachineId = 1, MachineStatusId = 3, RemovedAt = null }, // Broken (status 3)
                new Machine { MachineId = 2, MachineStatusId = 3, RemovedAt = null }  // Broken (status 3)
            );
            await _context.SaveChangesAsync();

            var thohResponse = """
                {
                    "orderId": 456,
                    "totalPrice": 20000,
                    "bankAccount": "thoh-bank-456"
                }
                """;

            SetupHttpResponse(HttpStatusCode.OK, thohResponse);
            _bankClientMock.Setup(x => x.MakePaymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                .ReturnsAsync("payment-456");

            // Act
            var result = await _service.EnsureMachinesAvailableAsync();

            // Assert
            Assert.True(result);
            _bankClientMock.Verify(x => x.MakePaymentAsync("thoh-bank-456", "thoh", 20000, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task EnsureMachinesAvailableAsync_WhenThohOrderFails_ShouldReturnFalse()
        {
            // Arrange - No machines in database
            SetupHttpResponse(HttpStatusCode.BadRequest, """{"error": "Order failed"}""");

            // Act
            var result = await _service.EnsureMachinesAvailableAsync();

            // Assert
            Assert.False(result);
            _bankClientMock.Verify(x => x.MakePaymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EnsureMachinesAvailableAsync_WhenPaymentFails_ShouldReturnFalse()
        {
            // Arrange - No machines in database
            var thohResponse = """
                {
                    "orderId": 789,
                    "totalPrice": 20000,
                    "bankAccount": "thoh-bank-789"
                }
                """;

            SetupHttpResponse(HttpStatusCode.OK, thohResponse);
            _bankClientMock.Setup(x => x.MakePaymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Payment failed"));

            // Act
            var result = await _service.EnsureMachinesAvailableAsync();

            // Assert
            Assert.False(result);
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
