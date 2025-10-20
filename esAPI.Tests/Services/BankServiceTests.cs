using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

using esAPI.Data;
using esAPI.Services;
using esAPI.Models;
using esAPI.Interfaces;
using esAPI.Clients;

namespace esAPI.Tests.Services
{
    public class BankServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICommercialBankClient> _mockBankClient;
        private readonly Mock<ISimulationStateService> _mockStateService;
        private readonly Mock<ILogger<BankService>> _mockLogger;
        private readonly Mock<RetryQueuePublisher> _mockRetryPublisher;
        private readonly BankService _service;

        public BankServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockBankClient = new Mock<ICommercialBankClient>();
            _mockStateService = new Mock<ISimulationStateService>();
            _mockLogger = new Mock<ILogger<BankService>>();
            // Mock configuration for RetryQueuePublisher
            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfiguration.Setup(c => c["Retry:QueueUrl"]).Returns("https://test-queue-url");

            _mockRetryPublisher = new Mock<RetryQueuePublisher>(
                Mock.Of<Amazon.SQS.IAmazonSQS>(),
                Mock.Of<ILogger<RetryQueuePublisher>>(),
                mockConfiguration.Object,
                _mockStateService.Object);

            _service = new BankService(
                _context,
                _mockBankClient.Object,
                _mockStateService.Object,
                _mockLogger.Object,
                _mockRetryPublisher.Object);
        }

        [Fact]
        public async Task GetAndStoreBalance_Success_ReturnsBalanceAndStoresSnapshot()
        {
            // Arrange
            const int simulationDay = 5;
            const decimal expectedBalance = 1500.75m;

            _mockBankClient.Setup(c => c.GetAccountBalanceAsync())
                .ReturnsAsync(expectedBalance);

            // Act
            var result = await _service.GetAndStoreBalance(simulationDay);

            // Assert
            result.Should().Be(expectedBalance);

            // Verify snapshot was stored
            var snapshot = await _context.BankBalanceSnapshots
                .FirstOrDefaultAsync(s => s.SimulationDay == simulationDay);

            snapshot.Should().NotBeNull();
            snapshot!.Balance.Should().Be((double)expectedBalance);
            snapshot.Timestamp.Should().Be(simulationDay);

            _mockBankClient.Verify(c => c.GetAccountBalanceAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAndStoreBalance_BankClientThrows_ReturnsSentinelValue()
        {
            // Arrange
            const int simulationDay = 3;

            _mockBankClient.Setup(c => c.GetAccountBalanceAsync())
                .ThrowsAsync(new HttpRequestException("Bank API unavailable"));

            // Act
            var result = await _service.GetAndStoreBalance(simulationDay);

            // Assert
            result.Should().Be(-1m); // Sentinel value

            // Verify no snapshot was stored
            var snapshots = await _context.BankBalanceSnapshots.ToListAsync();
            snapshots.Should().BeEmpty();

            // Note: Can't verify non-virtual method PublishAsync, but we can verify the result
            // The service should return sentinel value when retry publisher is available
        }

        [Fact]
        public async Task GetAndStoreBalance_DatabaseError_ReturnsSentinelValue()
        {
            // Arrange
            const int simulationDay = 7;
            const decimal expectedBalance = 2500.00m;

            _mockBankClient.Setup(c => c.GetAccountBalanceAsync())
                .ReturnsAsync(expectedBalance);

            // Dispose the context to simulate database error
            _context.Dispose();

            // Act
            var result = await _service.GetAndStoreBalance(simulationDay);

            // Assert - Should return sentinel value on database error
            result.Should().Be(-1m);
        }

        [Fact]
        public async Task GetAndStoreBalance_WithoutRetryPublisher_HandlesErrorGracefully()
        {
            // Arrange
            var serviceWithoutRetry = new BankService(
                _context,
                _mockBankClient.Object,
                _mockStateService.Object,
                _mockLogger.Object,
                null); // No retry publisher

            _mockBankClient.Setup(c => c.GetAccountBalanceAsync())
                .ThrowsAsync(new Exception("Network error"));

            // Act
            var result = await serviceWithoutRetry.GetAndStoreBalance(1);

            // Assert
            result.Should().Be(-1m); // Sentinel value

            // Should log warning about retry not available
            VerifyLogContains(LogLevel.Warning, "Retry functionality not available");
        }

        [Fact]
        public async Task GetAndStoreBalance_MultipleCallsSameDay_CreatesMultipleSnapshots()
        {
            // Arrange
            const int simulationDay = 2;
            const decimal balance1 = 1000m;
            const decimal balance2 = 1200m;

            _mockBankClient.SetupSequence(c => c.GetAccountBalanceAsync())
                .ReturnsAsync(balance1)
                .ReturnsAsync(balance2);

            // Act
            var result1 = await _service.GetAndStoreBalance(simulationDay);
            var result2 = await _service.GetAndStoreBalance(simulationDay);

            // Assert
            result1.Should().Be(balance1);
            result2.Should().Be(balance2);

            // Should have 2 snapshots for the same day
            var snapshots = await _context.BankBalanceSnapshots
                .Where(s => s.SimulationDay == simulationDay)
                .ToListAsync();

            snapshots.Should().HaveCount(2);
            snapshots[0].Balance.Should().Be((double)balance1);
            snapshots[1].Balance.Should().Be((double)balance2);
        }

        [Fact]
        public async Task GetAndStoreBalance_DifferentSimulationDays_StoresCorrectly()
        {
            // Arrange
            const decimal balance = 5000m;
            _mockBankClient.Setup(c => c.GetAccountBalanceAsync())
                .ReturnsAsync(balance);

            // Act
            await _service.GetAndStoreBalance(1);
            await _service.GetAndStoreBalance(3);
            await _service.GetAndStoreBalance(10);

            // Assert
            var snapshots = await _context.BankBalanceSnapshots
                .OrderBy(s => s.SimulationDay)
                .ToListAsync();

            snapshots.Should().HaveCount(3);
            snapshots[0].SimulationDay.Should().Be(1);
            snapshots[1].SimulationDay.Should().Be(3);
            snapshots[2].SimulationDay.Should().Be(10);

            snapshots.Should().AllSatisfy(s =>
            {
                s.Balance.Should().Be((double)balance);
                s.Timestamp.Should().Be(s.SimulationDay);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(int.MinValue)]
        public async Task GetAndStoreBalance_InvalidSimulationDay_StillProcesses(int invalidDay)
        {
            // Arrange
            const decimal balance = 100m;
            _mockBankClient.Setup(c => c.GetAccountBalanceAsync())
                .ReturnsAsync(balance);

            // Act
            var result = await _service.GetAndStoreBalance(invalidDay);

            // Assert
            result.Should().Be(balance);

            var snapshot = await _context.BankBalanceSnapshots
                .FirstOrDefaultAsync(s => s.SimulationDay == invalidDay);
            snapshot.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAndStoreBalance_ConcurrentCalls_AllSucceed()
        {
            // Arrange
            const decimal balance = 3000m;
            _mockBankClient.Setup(c => c.GetAccountBalanceAsync())
                .ReturnsAsync(balance);

            // Act - Concurrent calls for different days
            var tasks = Enumerable.Range(1, 5)
                .Select(day => _service.GetAndStoreBalance(day))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllSatisfy(r => r.Should().Be(balance));

            var snapshots = await _context.BankBalanceSnapshots.ToListAsync();
            snapshots.Should().HaveCount(5);

            _mockBankClient.Verify(c => c.GetAccountBalanceAsync(), Times.Exactly(5));
        }

        private void VerifyLogContains(LogLevel level, string message)
        {
            _mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
