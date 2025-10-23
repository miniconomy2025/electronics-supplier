

using System.Net;
using System.Text.Json;
using esAPI.Clients;
using esAPI.Data;
using esAPI.Interfaces;
using esAPI.Models;
using esAPI.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace esAPI.Tests.Services
{
    public class BankAccountServiceUnitTests : IDisposable
    {
        private readonly Mock<ICommercialBankClient> _mockBankClient;
        private readonly Mock<ILogger<BankAccountService>> _mockLogger;
        private readonly Mock<ISimulationStateService> _mockStateService;
        private readonly Mock<RetryQueuePublisher> _mockRetryQueuePublisher;
        private readonly AppDbContext _dbContext;
        private readonly BankAccountService _service;

        public BankAccountServiceUnitTests()
        {
            _mockBankClient = new Mock<ICommercialBankClient>();
            _mockLogger = new Mock<ILogger<BankAccountService>>();
            _mockStateService = new Mock<ISimulationStateService>();

            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfiguration.Setup(c => c["Retry:QueueUrl"]).Returns("https://test-queue-url");

            _mockRetryQueuePublisher = new Mock<RetryQueuePublisher>(
                Mock.Of<Amazon.SQS.IAmazonSQS>(),
                Mock.Of<ILogger<RetryQueuePublisher>>(),
                mockConfiguration.Object,
                _mockStateService.Object);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
            _dbContext = new AppDbContext(options);
            _dbContext.Companies.Add(new Company { CompanyId = 1, CompanyName = "Test" });
            _dbContext.SaveChanges();

            _service = new BankAccountService(_dbContext, _mockBankClient.Object, _mockLogger.Object, _mockRetryQueuePublisher.Object);
        }

        [Fact]
        public async Task SetupBankAccountAsync_WhenCreateFailsWithConflictAndGetSucceeds_ReturnsExistingAccount()
        {
            // Arrange
            var existingAccountNumber = "ACC-EXISTING-456";
            var conflictResponse = new HttpResponseMessage(HttpStatusCode.Conflict);
            var getAccountResponseJson = JsonSerializer.Serialize(new { account_number = existingAccountNumber });
            var getAccountHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(getAccountResponseJson)
            };

            _mockBankClient.Setup(c => c.CreateAccountAsync(It.IsAny<object>())).ReturnsAsync(conflictResponse);
            _mockBankClient.Setup(c => c.GetAccountAsync()).ReturnsAsync(getAccountHttpResponse);

            // Act
            var (success, resultAccountNumber, error) = await _service.SetupBankAccountAsync();

            // Assert
            success.Should().BeTrue();
            resultAccountNumber.Should().Be(existingAccountNumber);
            error.Should().BeNull();
            _mockBankClient.Verify(c => c.CreateAccountAsync(It.IsAny<object>()), Times.Once);
            _mockBankClient.Verify(c => c.GetAccountAsync(), Times.Once);
        }

        [Fact]
        public async Task SetupBankAccountAsync_WhenConflictAndGetFails_EnqueuesRetryAndReturnsFailure()
        {
            // Arrange
            var conflictResponse = new HttpResponseMessage(HttpStatusCode.Conflict);
            var getAccountErrorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            _mockBankClient.Setup(c => c.CreateAccountAsync(It.IsAny<object>())).ReturnsAsync(conflictResponse);
            _mockBankClient.Setup(c => c.GetAccountAsync()).ReturnsAsync(getAccountErrorResponse);

            // Act
            var (success, resultAccountNumber, error) = await _service.SetupBankAccountAsync();

            // Assert
            success.Should().BeFalse();
            resultAccountNumber.Should().BeNull();
            error.Should().Contain("retry scheduled");

            _mockRetryQueuePublisher.Verify(p => p.PublishAsync(It.IsAny<BankAccountRetryJob>()), Times.Once);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
