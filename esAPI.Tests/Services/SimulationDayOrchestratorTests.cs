using System.Collections.Generic;
using System.Threading.Tasks;
using esAPI.DTOs;
using esAPI.DTOs.Electronics;
using esAPI.Services;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using esAPI.Models;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Threading;
using System.Text;

namespace esAPI.Tests.Services
{
    public class SimulationDayOrchestratorTests
    {
        [Fact]
        public async Task OrchestrateAsync_HappyPath_CreatesOrFindsBankAccount()
        {
            // Arrange
            var mockBankClient = new Mock<ICommercialBankClient>();
            var mockStateService = new Mock<ISimulationStateService>();
            var mockLoggerBank = new Mock<ILogger<BankService>>();
            // Mock dependencies for RetryQueuePublisher
            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfiguration.Setup(c => c["Retry:QueueUrl"]).Returns("https://test-queue-url");

            var mockLogger = new Mock<ILogger<RetryQueuePublisher>>();
            var mockSqs = new Mock<Amazon.SQS.IAmazonSQS>();

            var retryPublisher = new RetryQueuePublisher(
                mockSqs.Object,
                mockLogger.Object,
                mockConfiguration.Object,
                mockStateService.Object);
            var mockLoggerOrchestrator = new Mock<ILogger<SimulationDayOrchestrator>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            var dbContext = new AppDbContext(options);

            // Setup HttpClientFactory to return a dummy HttpClient
            var handler = new HttpMessageHandlerStub();
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test-bank-api.com")
            };
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Setup bank client
            mockBankClient.Setup(b => b.GetAccountDetailsAsync()).ReturnsAsync("123456789");

            var bankService = new BankService(
                dbContext,
                mockBankClient.Object,
                mockStateService.Object,
                mockLoggerBank.Object,
                retryPublisher
            );

            var orchestrator = new SimulationDayOrchestrator(
                mockBankClient.Object,
                dbContext,
                mockHttpClientFactory.Object,
                mockLoggerOrchestrator.Object
            );

            // Act
            var result = await orchestrator.OrchestrateAsync();

            // Assert
            result.Should().NotBeNull();
            // You can add more asserts based on expected behavior
        }

        // Helper stub for HttpMessageHandler
        private class HttpMessageHandlerStub : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Simulate a Created response for /account
                if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/account")
                {
                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.Created)
                    {
                        Content = new StringContent("{\"account_number\":\"123456789\"}")
                    };
                    return Task.FromResult(response);
                }
                // Default response
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }
    }
}
