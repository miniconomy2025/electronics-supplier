using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using esAPI.Controllers;
using esAPI.Services;
using esAPI.DTOs.Electronics;

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
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            
            var returnedDetails = okResult.Value.Should().BeOfType<ElectronicsDetailsDto>().Subject;
            returnedDetails.AvailableStock.Should().Be(150);
            returnedDetails.PricePerUnit.Should().Be(25.50m);
            
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
            var notFoundResult = result.Result.Should().BeOfType<NotFoundResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            
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
            
            exception.Message.Should().Be("Database connection failed");
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
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            
            var returnedDetails = okResult.Value.Should().BeOfType<ElectronicsDetailsDto>().Subject;
            returnedDetails.AvailableStock.Should().Be(availableStock);
            returnedDetails.PricePerUnit.Should().Be(pricePerUnit);
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
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDetails = okResult.Value.Should().BeOfType<ElectronicsDetailsDto>().Subject;
            
            // Verify response matches swagger schema
            returnedDetails.Should().NotBeNull();
            returnedDetails.AvailableStock.Should().BeOfType<int>("availableStock should be integer");
            returnedDetails.PricePerUnit.Should().BeOfType<decimal>("pricePerUnit should be number");
            
            // Verify property values are reasonable
            returnedDetails.AvailableStock.Should().BeGreaterOrEqualTo(0, "stock cannot be negative");
            returnedDetails.PricePerUnit.Should().BeGreaterOrEqualTo(0, "price cannot be negative");
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
