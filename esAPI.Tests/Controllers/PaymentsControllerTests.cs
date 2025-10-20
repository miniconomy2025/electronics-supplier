using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using Xunit;
using FluentAssertions;

using esAPI.Controllers;
using esAPI.Data;
using esAPI.DTOs;
using esAPI.Models;
using esAPI.Interfaces;

namespace esAPI.Tests.Controllers
{
    public class PaymentsControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ISimulationStateService> _mockSimulationStateService;
        private readonly PaymentsController _controller;

        public PaymentsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockSimulationStateService = new Mock<ISimulationStateService>();
            _controller = new PaymentsController(_context, _mockSimulationStateService.Object);

            SeedTestData();
        }

        [Fact]
        public async Task ReceivePayment_ValidPayment_ReturnsOk()
        {
            // Arrange
            var paymentDto = new PaymentNotificationDto
            {
                TransactionNumber = "TXN123456",
                Status = "COMPLETED",
                Amount = 100.50m,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Description = "Test payment",
                From = "customer-account",
                To = "supplier-account"
            };

            // Act
            var result = await _controller.ReceivePayment(paymentDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult?.Value.Should().BeEquivalentTo(new { success = true });

            // Verify payment was saved
            var savedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionNumber == "TXN123456");
            savedPayment.Should().NotBeNull();
            savedPayment!.Amount.Should().Be(100.50m);
            savedPayment.Status.Should().Be("COMPLETED");
        }

        [Fact]
        public async Task ReceivePayment_WithOrderIdInDescription_UpdatesOrderStatus()
        {
            // Arrange
            var order = new ElectronicsOrder
            {
                OrderId = 123,
                TotalAmount = 100,
                OrderStatusId = 1 // Pending status
            };
            _context.ElectronicsOrders.Add(order);

            var acceptedStatus = new OrderStatus { StatusId = 2, Status = "ACCEPTED" };
            _context.OrderStatuses.Add(acceptedStatus);
            await _context.SaveChangesAsync();

            var paymentDto = new PaymentNotificationDto
            {
                TransactionNumber = "TXN789",
                Status = "COMPLETED",
                Amount = 150.00m, // More than order amount
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Description = "Payment for Order #123",
                From = "customer-account",
                To = "supplier-account"
            };

            _mockSimulationStateService.Setup(s => s.GetCurrentSimulationTime(It.IsAny<int>()))
                .Returns(1642678800.0m);

            // Act
            var result = await _controller.ReceivePayment(paymentDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify order status updated
            var updatedOrder = await _context.ElectronicsOrders.FindAsync(123);
            updatedOrder.Should().NotBeNull();
            updatedOrder!.OrderStatusId.Should().Be(2); // ACCEPTED

            // Verify payment linked to order
            var savedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionNumber == "TXN789");
            savedPayment.Should().NotBeNull();
            savedPayment!.OrderId.Should().Be(123);
        }

        [Fact]
        public async Task ReceivePayment_InsufficientAmount_DoesNotUpdateOrderStatus()
        {
            // Arrange
            var order = new ElectronicsOrder
            {
                OrderId = 456,
                TotalAmount = 100,
                OrderStatusId = 1 // Pending status
            };
            _context.ElectronicsOrders.Add(order);
            await _context.SaveChangesAsync();

            var paymentDto = new PaymentNotificationDto
            {
                TransactionNumber = "TXN456",
                Status = "COMPLETED",
                Amount = 50.00m, // Less than order amount
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Description = "Partial payment for Order #456",
                From = "customer-account",
                To = "supplier-account"
            };

            // Act
            var result = await _controller.ReceivePayment(paymentDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify order status NOT updated
            var orderAfter = await _context.ElectronicsOrders.FindAsync(456);
            orderAfter.Should().NotBeNull();
            orderAfter!.OrderStatusId.Should().Be(1); // Still pending

            // Verify payment still saved
            var savedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionNumber == "TXN456");
            savedPayment.Should().NotBeNull();
            savedPayment!.OrderId.Should().Be(456);
        }

        [Fact]
        public async Task ReceivePayment_InvalidOrderId_StillSavesPayment()
        {
            // Arrange
            var paymentDto = new PaymentNotificationDto
            {
                TransactionNumber = "TXN999",
                Status = "COMPLETED",
                Amount = 75.00m,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Description = "Payment for Order #999", // Non-existent order
                From = "customer-account",
                To = "supplier-account"
            };

            // Act
            var result = await _controller.ReceivePayment(paymentDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify payment saved without order link
            var savedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionNumber == "TXN999");
            savedPayment.Should().NotBeNull();
            savedPayment!.OrderId.Should().BeNull();
        }

        [Fact]
        public async Task ReceivePayment_UpdatesElectronicsWhenOrderAccepted()
        {
            // Arrange
            var order = new ElectronicsOrder
            {
                OrderId = 789,
                TotalAmount = 2, // Order for 2 electronics
                OrderStatusId = 1
            };
            _context.ElectronicsOrders.Add(order);

            // Add reserved electronics
            var electronics1 = new Electronic
            {
                ElectronicId = 1,
                ElectronicsStatusId = (int)esAPI.Models.Enums.Electronics.Status.Reserved,
                ProducedAt = 1000m,
                SoldAt = null
            };
            var electronics2 = new Electronic
            {
                ElectronicId = 2,
                ElectronicsStatusId = (int)esAPI.Models.Enums.Electronics.Status.Reserved,
                ProducedAt = 1100m,
                SoldAt = null
            };
            _context.Electronics.AddRange(electronics1, electronics2);

            var acceptedStatus = new OrderStatus { StatusId = 2, Status = "ACCEPTED" };
            _context.OrderStatuses.Add(acceptedStatus);
            await _context.SaveChangesAsync();

            var paymentDto = new PaymentNotificationDto
            {
                TransactionNumber = "TXN789",
                Status = "COMPLETED",
                Amount = 200.00m,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Description = "Payment for Order #789",
                From = "customer-account",
                To = "supplier-account"
            };

            _mockSimulationStateService.Setup(s => s.GetCurrentSimulationTime(It.IsAny<int>()))
                .Returns(2000.0m);

            // Act
            var result = await _controller.ReceivePayment(paymentDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify electronics updated with SoldAt timestamp
            var updatedElectronics = await _context.Electronics
                .Where(e => e.SoldAt != null)
                .ToListAsync();
            updatedElectronics.Should().HaveCount(2);
            updatedElectronics.Should().AllSatisfy(e => e.SoldAt.Should().Be(2000m));
        }

        private void SeedTestData()
        {
            // Add basic test data if needed
            var pendingStatus = new OrderStatus { StatusId = 1, Status = "PENDING" };
            _context.OrderStatuses.Add(pendingStatus);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
