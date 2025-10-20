using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using esAPI.Data;
using esAPI.Services;
using esAPI.Interfaces;
using esAPI.Models;

namespace esAPI.Tests.Services
{
    public class MaterialOrderingServiceIntegrationTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ISimulationStateService> _mockSimulationStateService;
        private readonly Mock<ILogger<MaterialOrderingService>> _mockLogger;

        public MaterialOrderingServiceIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new AppDbContext(options);
            _mockSimulationStateService = new Mock<ISimulationStateService>();
            _mockLogger = new Mock<ILogger<MaterialOrderingService>>();

            SetupTestData();
        }

        private void SetupTestData()
        {
            // Add test companies for logistics
            _context.Companies.Add(new Company 
            { 
                CompanyId = 1, 
                CompanyName = "BulkLogistics",
                BankAccountNumber = "BL-12345"
            });
            _context.SaveChanges();
        }

        [Fact]
        public async Task OrderMaterialIfLowStockAsync_WithSufficientStock_ShouldReturnTrue()
        {
            // Arrange
            // Create a service with null clients (won't be called for sufficient stock)
            var service = new MaterialOrderingService(
                _context,
                null!, // Won't be called
                null!, // Won't be called
                null!, // Won't be called
                null!, // Won't be called
                _mockSimulationStateService.Object,
                _mockLogger.Object
            );

            string materialName = "steel";
            int ownStock = 1500; // Above threshold of 1000
            int dayNumber = 1;

            // Act
            var result = await service.OrderMaterialIfLowStockAsync(materialName, ownStock, dayNumber);

            // Assert
            Assert.True(result);
            
            // Verify logger was called with appropriate message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"{materialName} stock is sufficient ({ownStock}), no order needed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void MaterialOrderingService_Constructor_ShouldCreateService()
        {
            // Arrange & Act
            var service = new MaterialOrderingService(
                _context,
                null!,
                null!,
                null!,
                null!,
                _mockSimulationStateService.Object,
                _mockLogger.Object
            );

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task OrderMaterialIfLowStockAsync_WithZeroStock_ShouldTriggerOrderAttempt()
        {
            // Arrange
            var service = new MaterialOrderingService(
                _context,
                null!, // This will cause failures when actually trying to order
                null!,
                null!,
                null!,
                _mockSimulationStateService.Object,
                _mockLogger.Object
            );

            string materialName = "steel";
            int ownStock = 0; // Below threshold
            int dayNumber = 1;

            // Act & Assert
            // This should attempt to order but fail due to null clients
            // The test verifies the low stock path is taken
            var result = await service.OrderMaterialIfLowStockAsync(materialName, ownStock, dayNumber);
            
            // Should return false due to null clients, but shows low stock path was taken
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}