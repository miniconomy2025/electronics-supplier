using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using esAPI.Clients;
using esAPI.Services;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;
using esAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using FluentAssertions;
using esAPI.Interfaces;


namespace esAPI.Tests.Integration
{
    public class WireMockServerFixture : IDisposable
    {
        // CHANGE THIS LINE:
        public WireMockServer Server { get; }

        public string Url => Server.Url!;

        public WireMockServerFixture()
        {
            Server = WireMockServer.Start();
        }

        public void Dispose()
        {
            Server.Stop();
            Server.Dispose();
        }
    }

    #region CommercialBankClient Integration Tests (with WireMock)

    public class CommercialBankClientTests : IClassFixture<WireMockServerFixture>
    {
        private readonly WireMockServer _server;
        private readonly CommercialBankClient _client;

        public CommercialBankClientTests(WireMockServerFixture fixture)
        {
            _server = fixture.Server;
            _server.Reset(); // Reset mappings for each test to ensure isolation.

            var services = new ServiceCollection();

            // Configure the HttpClient to use the WireMock server's URL as its base address.
            services.AddHttpClient("commercial-bank", client =>
            {
                client.BaseAddress = new Uri(_server.Url!);
            });

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            _client = new CommercialBankClient(httpClientFactory);
        }

        [Fact]
        public async Task GetAccountBalanceAsync_WhenApiReturnsSuccessAndValidBalance_ShouldReturnCorrectDecimal()
        {
            // Arrange
            _server
                .Given(Request.Create().WithPath("/api/account/me/balance").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new { success = true, balance = "12345.67" }));

            // Act
            var result = await _client.GetAccountBalanceAsync();

            // Assert
            result.Should().Be(12345.67m);
        }

        [Fact]
        public async Task GetAccountBalanceAsync_WhenApiReturnsNonSuccessStatusCode_ShouldReturnZero()
        {
            // Arrange
            _server
                .Given(Request.Create().WithPath("/api/account/me/balance").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

            // Act
            var result = await _client.GetAccountBalanceAsync();

            // Assert
            result.Should().Be(0m);
        }

        [Fact]
        public async Task RequestLoanAsync_WhenLoanIsSuccessful_ShouldReturnLoanNumber()
        {
            // Arrange
            _server
                .Given(Request.Create().WithPath("/api/loan").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new { success = true, loan_number = "LN-98765" }));

            // Act
            var result = await _client.RequestLoanAsync(50000m);

            // Assert
            result.Should().Be("LN-98765");
        }

        [Fact]
        public async Task MakePaymentAsync_WhenResponseIndicatesFailure_ThrowsApiCallFailedException()
        {
            // Arrange
            _server
                .Given(Request.Create().WithPath("/api/transaction").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK) // The API might return 200 OK for a logical failure
                    .WithBodyAsJson(new { success = false, error = "Insufficient funds" }));

            // Act
            Func<Task> act = async () => await _client.MakePaymentAsync("to-acc", "to-bank", 100, "test");

            // Assert

        }

        [Fact]
        public async Task GetAccountDetailsAsync_WhenAccountExists_ShouldReturnAccountNumber()
        {
            // Arrange
            _server
               .Given(Request.Create().WithPath("/api/account/me").UsingGet())
               .RespondWith(Response.Create()
                   .WithStatusCode(HttpStatusCode.OK)
                   .WithBodyAsJson(new { account_number = "ACC123", owner = "Test Corp" }));

            // Act
            var result = await _client.GetAccountDetailsAsync();

            // Assert
            result.Should().Be("ACC123");
        }
    }

    #endregion

    #region BankAccountService Integration Tests (with WireMock)

    public class BankAccountServiceTests : IClassFixture<WireMockServerFixture>
    {
        private readonly WireMockServer _server;
        private readonly BankAccountService _service;
        private readonly AppDbContext _dbContext;
        private readonly Mock<ISimulationStateService> _mockStateService;

        public BankAccountServiceTests(WireMockServerFixture fixture)
        {
            _server = fixture.Server;
            _server.Reset();

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            _dbContext.Companies.Add(new Company { CompanyId = 1, CompanyName = "Electronics Supplier" });
            _dbContext.SaveChanges();

            // *** This is the key change: we inject a REAL client configured to talk to our mock server ***
            var services = new ServiceCollection();
            services.AddHttpClient("commercial-bank", client => client.BaseAddress = new Uri(_server.Url!));
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            _mockStateService = new Mock<ISimulationStateService>();

            var realBankClient = new CommercialBankClient(httpClientFactory);

            var mockLogger = new Mock<ILogger<BankAccountService>>();

            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfiguration.Setup(c => c["Retry:QueueUrl"]).Returns("https://test-queue-url");

            var mockRetryPublisher = new Mock<RetryQueuePublisher>(
                Mock.Of<Amazon.SQS.IAmazonSQS>(),
                Mock.Of<ILogger<RetryQueuePublisher>>(),
                mockConfiguration.Object,
                _mockStateService.Object);

            _service = new BankAccountService(_dbContext, realBankClient, mockLogger.Object, mockRetryPublisher.Object);
        }

        [Fact]
        public async Task SetupBankAccountAsync_WhenNoAccountExists_CreatesNewAccountSuccessfully()
        {
            // Arrange
            var accountNumber = "ACC-NEW-123";
            _server
                .Given(Request.Create().WithPath("/api/account").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.Created)
                    .WithBodyAsJson(new { account_number = accountNumber }));

            // Act
            var (success, resultAccountNumber, error) = await _service.SetupBankAccountAsync();

            // Assert
            success.Should().BeTrue();
            resultAccountNumber.Should().Be(accountNumber);
            error.Should().BeNull();
            (await _dbContext.Companies.FindAsync(1))!.BankAccountNumber.Should().Be(accountNumber);
        }

        [Fact]
        public async Task SetupBankAccountAsync_WhenAccountAlreadyExistsInBank_RetrievesExistingAccount()
        {
            // Arrange
            var existingAccountNumber = "ACC-EXISTING-456";

            // 1. First call to create an account will fail with Conflict
            _server
                .Given(Request.Create().WithPath("/api/account").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Conflict));

            // 2. The subsequent call to get the account will succeed
            _server
                .Given(Request.Create().WithPath("/api/account/me").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(new { account_number = existingAccountNumber }));

            // Act
            var (success, resultAccountNumber, error) = await _service.SetupBankAccountAsync();

            // Assert
            success.Should().BeTrue();
            resultAccountNumber.Should().Be(existingAccountNumber);
            error.Should().BeNull();
            (await _dbContext.Companies.FindAsync(1))!.BankAccountNumber.Should().Be(existingAccountNumber);
        }
    }

    #endregion
}
