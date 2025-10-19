using esAPI.Data;
using esAPI.Models;
using esAPI.Models.Enums;
using esAPI.Services;
using esAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace esAPI.Tests.Services
{
    public class OrderExpirationServiceTests
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public OrderExpirationServiceTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task ReserveElectronicsForOrderAsync_WithAvailableElectronics_ShouldReserveCorrectly()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var mockStateService = new Mock<ISimulationStateService>();
            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton(mockStateService.Object);
            var provider = services.BuildServiceProvider();
            var service = new OrderExpirationService(provider, mockStateService.Object);

            // Add some available electronics
            var electronics = new List<Electronic>
            {
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available }
            };
            context.Electronics.AddRange(electronics);
            await context.SaveChangesAsync();

            // Act
            var result = await service.ReserveElectronicsForOrderAsync(1, 2);

            // Assert
            Assert.True(result);
            var reservedCount = await context.Electronics
                .Where(e => e.ElectronicsStatusId == (int)Electronics.Status.Reserved)
                .CountAsync();
            Assert.Equal(2, reservedCount);
        }

        [Fact]
        public async Task ReserveElectronicsForOrderAsync_WithInsufficientElectronics_ShouldReturnFalse()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var mockStateService = new Mock<ISimulationStateService>();
            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton(mockStateService.Object);
            var provider = services.BuildServiceProvider();
            var service = new OrderExpirationService(provider, mockStateService.Object);

            // Add only 1 available electronics
            var electronics = new List<Electronic>
            {
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available }
            };
            context.Electronics.AddRange(electronics);
            await context.SaveChangesAsync();

            // Act
            var result = await service.ReserveElectronicsForOrderAsync(1, 2);

            // Assert
            Assert.False(result);
            var reservedCount = await context.Electronics
                .Where(e => e.ElectronicsStatusId == (int)Electronics.Status.Reserved)
                .CountAsync();
            Assert.Equal(0, reservedCount);
        }

        [Fact]
        public async Task GetAvailableElectronicsCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var mockStateService = new Mock<ISimulationStateService>();
            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton(mockStateService.Object);
            var provider = services.BuildServiceProvider();
            var service = new OrderExpirationService(provider, mockStateService.Object);

            // Add electronics with different statuses
            var electronics = new List<Electronic>
            {
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Reserved },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available, SoldAt = 2.0m }
            };
            context.Electronics.AddRange(electronics);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetAvailableElectronicsCountAsync();

            // Assert
            Assert.Equal(2, result); // Only 2 available (not reserved, not sold)
        }

        [Fact]
        public async Task GetReservedElectronicsCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var mockStateService = new Mock<ISimulationStateService>();
            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton(mockStateService.Object);
            var provider = services.BuildServiceProvider();
            var service = new OrderExpirationService(provider, mockStateService.Object);

            // Add electronics with different statuses
            var electronics = new List<Electronic>
            {
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Available },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Reserved },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Reserved },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Reserved, SoldAt = 2.0m }
            };
            context.Electronics.AddRange(electronics);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetReservedElectronicsCountAsync();

            // Assert
            Assert.Equal(2, result); // Only 2 reserved (not sold)
        }

        [Fact]
        public async Task CheckAndExpireOrdersAsync_WithExpiredOrders_ShouldExpireThem()
        {
            // Arrange
            using var context = new AppDbContext(_options);
            var mockStateService = new Mock<ISimulationStateService>();
            mockStateService.Setup(x => x.IsRunning).Returns(true);
            mockStateService.Setup(x => x.GetCurrentSimulationTime(3)).Returns(3.0m);

            var services = new ServiceCollection();
            services.AddSingleton(context);
            services.AddSingleton(mockStateService.Object);
            var provider = services.BuildServiceProvider();
            var service = new OrderExpirationService(provider, mockStateService.Object);

            // Add an expired order (ordered 2 days ago, current time is 3.0)
            var order = new ElectronicsOrder
            {
                OrderId = 1,
                OrderStatusId = (int)Order.Status.Pending,
                OrderedAt = 1.0m, // 2 days ago
                TotalAmount = 10,
                RemainingAmount = 10
            };
            context.ElectronicsOrders.Add(order);

            // Add a price per unit so the expiration logic works
            context.LookupValues.Add(new LookupValue
            {
                ElectronicsPricePerUnit = 10m,
                ChangedAt = 0m
            });

            // Add some reserved electronics
            var electronics = new List<Electronic>
            {
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Reserved },
                new Electronic { ProducedAt = 1.0m, ElectronicsStatusId = (int)Electronics.Status.Reserved }
            };
            context.Electronics.AddRange(electronics);
            await context.SaveChangesAsync();

            // Act
            var result = await service.CheckAndExpireOrdersAsync();

            // Assert
            Assert.Equal(1, result);
            var expiredOrder = await context.ElectronicsOrders.FindAsync(1);
            Assert.Equal((int)Order.Status.Expired, expiredOrder!.OrderStatusId);
        }
    }
}