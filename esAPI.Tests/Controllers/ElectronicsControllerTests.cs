using Microsoft.AspNetCore.Mvc;
using Moq;
using esAPI.Controllers;
using esAPI.DTOs.Electronics;
using esAPI.Interfaces;
using Xunit;

namespace esAPI.Tests.Controllers
{
    public class ElectronicsControllerTests
    {
        private readonly Mock<IElectronicsService> _mockElectronicsService;
        private readonly ElectronicsController _controller;

        public ElectronicsControllerTests()
        {
            _mockElectronicsService = new Mock<IElectronicsService>();
            _controller = new ElectronicsController(_mockElectronicsService.Object);
        }

        [Fact]
        public async Task GetElectronics_WhenElectronicsDetailsExist_ReturnsOkWithCorrectData()
        {
            // Arrange
            var expectedDetails = new ElectronicsDetailsDto
            {
                AvailableStock = 150,
                PricePerUnit = 25.50m
            };

            _mockElectronicsService
                .Setup(s => s.GetElectronicsDetailsAsync())
                .ReturnsAsync(expectedDetails);

            // Act
            var result = await _controller.GetElectronics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);

            var returnedDetails = Assert.IsType<ElectronicsDetailsDto>(okResult.Value);
            Assert.Equal(150, returnedDetails.AvailableStock);
            Assert.Equal(25.50m, returnedDetails.PricePerUnit);

            _mockElectronicsService.Verify(s => s.GetElectronicsDetailsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetElectronics_WhenElectronicsDetailsAreNull_ReturnsNotFound()
        {
            // Arrange
            _mockElectronicsService
                .Setup(s => s.GetElectronicsDetailsAsync())
                .ReturnsAsync((ElectronicsDetailsDto?)null);

            // Act
            var result = await _controller.GetElectronics();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
            _mockElectronicsService.Verify(s => s.GetElectronicsDetailsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetElectronics_WhenServiceThrowsException_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database connection failed");
            _mockElectronicsService
                .Setup(s => s.GetElectronicsDetailsAsync())
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.GetElectronics());
            Assert.Equal("Database connection failed", exception.Message);
            _mockElectronicsService.Verify(s => s.GetElectronicsDetailsAsync(), Times.Once);
        }

        [Theory]
        [InlineData(0, 25.50)] // No stock available
        [InlineData(1, 25.50)] // Single unit
        [InlineData(1000, 30.00)] // Large stock with different price
        [InlineData(50, 0)] // Free electronics (edge case)
        public async Task GetElectronics_WithVariousStockAndPriceValues_ReturnsCorrectData(
            int availableStock, decimal pricePerUnit)
        {
            // Arrange
            var expectedDetails = new ElectronicsDetailsDto
            {
                AvailableStock = availableStock,
                PricePerUnit = pricePerUnit
            };

            _mockElectronicsService
                .Setup(s => s.GetElectronicsDetailsAsync())
                .ReturnsAsync(expectedDetails);

            // Act
            var result = await _controller.GetElectronics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            var returnedDetails = Assert.IsType<ElectronicsDetailsDto>(okResult.Value);
            Assert.Equal(availableStock, returnedDetails.AvailableStock);
            Assert.Equal(pricePerUnit, returnedDetails.PricePerUnit);
        }

        [Fact]
        public async Task GetElectronics_ResponseMatchesSwaggerSchema_HasCorrectProperties()
        {
            // Arrange
            var expectedDetails = new ElectronicsDetailsDto
            {
                AvailableStock = 75,
                PricePerUnit = 28.99m
            };

            _mockElectronicsService
                .Setup(s => s.GetElectronicsDetailsAsync())
                .ReturnsAsync(expectedDetails);

            // Act
            var result = await _controller.GetElectronics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDetails = Assert.IsType<ElectronicsDetailsDto>(okResult.Value);
            // Verify response matches swagger schema
            Assert.NotNull(returnedDetails);
            Assert.IsType<int>(returnedDetails.AvailableStock);
            Assert.IsType<decimal>(returnedDetails.PricePerUnit);
            // Verify property values are reasonable
            Assert.True(returnedDetails.AvailableStock >= 0, "stock cannot be negative");
            Assert.True(returnedDetails.PricePerUnit >= 0, "price cannot be negative");
        }

        [Fact]
        public async Task GetElectronics_ServiceMethodCalledCorrectly_VerifyInteraction()
        {
            // Arrange
            var expectedDetails = new ElectronicsDetailsDto
            {
                AvailableStock = 100,
                PricePerUnit = 25.50m
            };

            _mockElectronicsService
                .Setup(s => s.GetElectronicsDetailsAsync())
                .ReturnsAsync(expectedDetails);

            // Act
            await _controller.GetElectronics();

            // Assert
            _mockElectronicsService.Verify(
                s => s.GetElectronicsDetailsAsync(),
                Times.Once,
                "GetElectronicsDetailsAsync should be called exactly once");

            _mockElectronicsService.VerifyNoOtherCalls();
        }
    }
}
