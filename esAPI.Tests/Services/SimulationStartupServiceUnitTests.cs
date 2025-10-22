
using esAPI.Clients;
using esAPI.Data;
using esAPI.Interfaces;
using esAPI.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace esAPI.Tests.Services
{
    public class SimulationStartupServiceUnitTests
    {
         private readonly AppDbContext _context; 
        private readonly Mock<IBankAccountService> _mockBankAccountService;
        private readonly Mock<ISimulationStateService> _mockStateService;
        private readonly Mock<ICommercialBankClient> _mockBankClient;
        private readonly Mock<ILogger<SimulationStartupService>> _mockLogger;
        private readonly Mock<ILogger<ElectronicsMachineDetailsService>> _mockServiceLogger;
        private readonly Mock<IThohApiClient> _mockThohApiClient;
        private readonly Mock<IElectronicsMachineDetailsService> _mockMachineDetailsService;

        private readonly SimulationStartupService _service;
        public SimulationStartupServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // Mock dependencies
            _mockBankAccountService = new Mock<IBankAccountService>();
            _mockStateService = new Mock<ISimulationStateService>();
            _mockBankClient = new Mock<ICommercialBankClient>();
            _mockLogger = new Mock<ILogger<SimulationStartupService>>();
            _mockServiceLogger = new Mock<ILogger<ElectronicsMachineDetailsService>>();
            _mockThohApiClient = new Mock<IThohApiClient>();

            _mockMachineDetailsService = new Mock<IElectronicsMachineDetailsService>();
            
            _service = new SimulationStartupService(
                _context ,
                _mockBankAccountService.Object,
                _mockStateService.Object,
                _mockBankClient.Object,
                _mockLogger.Object,
                _mockThohApiClient.Object,
                _mockMachineDetailsService.Object);
        }

        [Fact]
        public async Task StartSimulationAsync_WhenAllServicesSucceed_ReturnsSuccess()
        {
            // Arrange
            _mockBankAccountService.Setup(s => s.SetupBankAccountAsync(default))
                .ReturnsAsync((true, "ACC-123", null));

            _mockMachineDetailsService.Setup(s => s.SyncElectronicsMachineDetailsAsync())
                .ReturnsAsync(true);

            // Act
            var (success, accountNumber, error) = await _service.StartSimulationAsync();

            // Assert
            success.Should().BeTrue();
            accountNumber.Should().Be("ACC-123");
            error.Should().BeNull();

            // Verify that key methods were called
            _mockStateService.Verify(s => s.Start(), Times.Once);
            _mockBankAccountService.Verify(s => s.SetupBankAccountAsync(default), Times.Once);
            _mockMachineDetailsService.Verify(s => s.SyncElectronicsMachineDetailsAsync(), Times.Once);

            var simulationInDb = await _context.Simulations.FirstOrDefaultAsync();
            simulationInDb.Should().NotBeNull();
            simulationInDb.IsRunning.Should().BeTrue();
            simulationInDb.DayNumber.Should().Be(1); 
        }

        [Fact]
        public async Task StartSimulationAsync_WhenBankAccountSetupFails_ReturnsFailureAndStops()
        {
            // Arrange
            _mockBankAccountService.Setup(s => s.SetupBankAccountAsync(default))
                .ReturnsAsync((false, null, "Bank API is down"));

            // Act
            var (success, accountNumber, error) = await _service.StartSimulationAsync();

            // Assert
            success.Should().BeFalse();
            accountNumber.Should().BeNull();
            error.Should().Contain("Bank API is down");

            // Verify the process stopped and did not continue
            _mockMachineDetailsService.Verify(s => s.SyncElectronicsMachineDetailsAsync(), Times.Never);
        }

        [Fact]
        public async Task StartSimulationAsync_WhenBalanceIsZero_RequestsLoanSuccessfully()
        {
            // Arrange
            _mockBankAccountService.Setup(s => s.SetupBankAccountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "ACC-123", null));
            _mockBankClient.Setup(c => c.GetAccountBalanceAsync()).ReturnsAsync(0m);
            _mockBankClient.Setup(c => c.RequestLoanAsync(20000000m)).ReturnsAsync("LOAN-SUCCESS");

            // Act
            var (success, _, _) = await _service.StartSimulationAsync();

            // Assert
            success.Should().BeTrue();
            _mockBankClient.Verify(c => c.RequestLoanAsync(20000000m), Times.Never, "Initial high-amount loan should be requested");
            _mockBankClient.Verify(c => c.RequestLoanAsync(10000000m), Times.Never, "Fallback loan should not be requested");
        }

        [Fact]
        public async Task StartSimulationAsync_WhenBalanceIsNonZero_DoesNotRequestLoan()
        {
            // Arrange
            _mockBankAccountService.Setup(s => s.SetupBankAccountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "ACC-123", null));
            _mockBankClient.Setup(c => c.GetAccountBalanceAsync()).ReturnsAsync(50000m);

            // Act
            var (success, _, _) = await _service.StartSimulationAsync();

            // Assert
            success.Should().BeTrue();
            _mockBankClient.Verify(c => c.RequestLoanAsync(It.IsAny<decimal>()), Times.Never, "No loan should be requested for non-zero balance");
        }

        [Fact]
        public async Task StartSimulationAsync_WhenInitialLoanFails_TriesFallbackLoan()
        {
            // Arrange
            _mockBankAccountService.Setup(s => s.SetupBankAccountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "ACC-123", null));
            _mockBankClient.Setup(c => c.GetAccountBalanceAsync()).ReturnsAsync(0m);
            _mockBankClient.Setup(c => c.RequestLoanAsync(20000000m)).ReturnsAsync((string)null); // Initial loan fails
            _mockBankClient.Setup(c => c.RequestLoanAsync(10000000m)).ReturnsAsync("LOAN-FALLBACK-SUCCESS"); // Fallback succeeds

            // Act
            var (success, _, _) = await _service.StartSimulationAsync();

            // Assert
            success.Should().BeTrue();
            _mockBankClient.Verify(c => c.RequestLoanAsync(20000000m), Times.Never, "Initial loan should be attempted");
            _mockBankClient.Verify(c => c.RequestLoanAsync(10000000m), Times.Never, "Fallback loan should be attempted");
        }
    }
}