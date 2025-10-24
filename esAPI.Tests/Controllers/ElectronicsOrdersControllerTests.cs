using esAPI.Controllers;
using esAPI.Data;
using esAPI.DTOs.Electronics;
using esAPI.DTOs.Orders;
using esAPI.Interfaces;
using esAPI.Models;
using esAPI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace esAPI.Tests.Controllers
{
    public class ElectronicsOrdersControllerTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<IElectronicsService> _mockElectronicsService;
        private readonly Mock<ISimulationStateService> _mockStateService;
        private readonly Mock<IOrderExpirationService> _mockOrderExpirationService;

        public ElectronicsOrdersControllerTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockElectronicsService = new Mock<IElectronicsService>();
            _mockStateService = new Mock<ISimulationStateService>();
            _mockOrderExpirationService = new Mock<IOrderExpirationService>();

            _mockStateService.Setup(s => s.GetCurrentSimulationTime(3)).Returns(1.500m);
        }

        private AppDbContext CreateContext()
        {
            var context = new AppDbContext(_options);
            SeedTestData(context);
            return context;
        }

        private void SeedTestData(AppDbContext context)
        {
            if (!context.Companies.Any())
            {
                context.Companies.AddRange(
                    new Company { CompanyId = 1, CompanyName = "electronics-supplier", BankAccountNumber = "123456789012" },
                    new Company { CompanyId = 2, CompanyName = "test-manufacturer", BankAccountNumber = "987654321098" }
                );
            }

            if (!context.OrderStatuses.Any())
            {
                context.OrderStatuses.AddRange(
                    new OrderStatus { StatusId = 1, Status = "PENDING" },
                    new OrderStatus { StatusId = 2, Status = "ACCEPTED" },
                    new OrderStatus { StatusId = 3, Status = "REJECTED" },
                    new OrderStatus { StatusId = 4, Status = "COMPLETED" }
                );
            }

            context.SaveChanges();
        }

        private ElectronicsOrdersController CreateController(AppDbContext context, Company? currentCompany = null)
        {
            var controller = new ElectronicsOrdersController(
                context,
                _mockElectronicsService.Object,
                _mockStateService.Object,
                _mockOrderExpirationService.Object);

            var httpContext = new DefaultHttpContext();
            if (currentCompany != null)
            {
                httpContext.Items["CurrentCompany"] = currentCompany;
                httpContext.Items["ClientId"] = currentCompany.CompanyName;
            }
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return controller;
        }

        [Fact]
        public async Task CreateOrder_WithValidRequest_ReturnsCreatedResult()
        {
            using var context = CreateContext();
            var manufacturer = context.Companies.First(c => c.CompanyId == 2);
            var controller = CreateController(context, manufacturer);

            var orderRequest = new ElectronicsOrderRequest { Quantity = 10 };

            _mockOrderExpirationService.Setup(s => s.GetAvailableElectronicsCountAsync())
                .ReturnsAsync(50);
            _mockOrderExpirationService.Setup(s => s.ReserveElectronicsForOrderAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            _mockElectronicsService.Setup(s => s.GetElectronicsDetailsAsync())
                .ReturnsAsync(new ElectronicsDetailsDto { AvailableStock = 50, PricePerUnit = 25.50m });

            var result = await controller.CreateOrder(orderRequest);

            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);

            var responseDto = createdResult.Value.Should().BeOfType<ElectronicsOrderResponseDto>().Subject;
            responseDto.Quantity.Should().Be(10);
            responseDto.AmountDue.Should().Be(255.00m);
            responseDto.BankNumber.Should().Be("123456789012");

            var savedOrder = await context.ElectronicsOrders.FirstOrDefaultAsync();
            savedOrder.Should().NotBeNull();
            savedOrder!.TotalAmount.Should().Be(10);
            savedOrder.RemainingAmount.Should().Be(10);
            savedOrder.ManufacturerId.Should().Be(2);
            savedOrder.OrderStatusId.Should().Be(1);
        }

        [Fact]
        public async Task CreateOrder_WithoutAuthentication_ReturnsUnauthorized()
        {
            using var context = CreateContext();
            var controller = CreateController(context, null);

            var orderRequest = new ElectronicsOrderRequest { Quantity = 10 };

            var result = await controller.CreateOrder(orderRequest);

            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
            unauthorizedResult.Value.Should().Be("You must be authenticated to place an order.");
        }

        [Fact]
        public async Task CreateOrder_WithInvalidQuantity_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var manufacturer = context.Companies.First(c => c.CompanyId == 2);
            var controller = CreateController(context, manufacturer);

            var orderRequest = new ElectronicsOrderRequest { Quantity = 0 };

            var result = await controller.CreateOrder(orderRequest);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("Invalid order data.");
        }

        [Fact]
        public async Task CreateOrder_WithInsufficientStock_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var manufacturer = context.Companies.First(c => c.CompanyId == 2);
            var controller = CreateController(context, manufacturer);

            var orderRequest = new ElectronicsOrderRequest { Quantity = 100 };

            _mockOrderExpirationService.Setup(s => s.GetAvailableElectronicsCountAsync())
                .ReturnsAsync(50);

            var result = await controller.CreateOrder(orderRequest);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("Insufficient stock available. Available: 50, Requested: 100");
        }

        [Fact]
        public async Task CreateOrder_WhenReservationFails_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var manufacturer = context.Companies.First(c => c.CompanyId == 2);
            var controller = CreateController(context, manufacturer);

            var orderRequest = new ElectronicsOrderRequest { Quantity = 10 };

            _mockOrderExpirationService.Setup(s => s.GetAvailableElectronicsCountAsync())
                .ReturnsAsync(50);
            _mockOrderExpirationService.Setup(s => s.ReserveElectronicsForOrderAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var result = await controller.CreateOrder(orderRequest);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("Failed to reserve electronics for the order. Please try again.");

            var savedOrder = await context.ElectronicsOrders.FirstOrDefaultAsync();
            savedOrder.Should().BeNull();
        }

        [Fact]
        public async Task GetAllOrders_ReturnsAllOrders()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var orders = new List<Models.ElectronicsOrder>
            {
                new Models.ElectronicsOrder { ManufacturerId = 2, TotalAmount = 10, RemainingAmount = 10, OrderedAt = 1.0m, OrderStatusId = 1 },
                new Models.ElectronicsOrder { ManufacturerId = 2, TotalAmount = 20, RemainingAmount = 15, OrderedAt = 1.5m, OrderStatusId = 2 }
            };
            context.ElectronicsOrders.AddRange(orders);
            await context.SaveChangesAsync();

            var result = await controller.GetAllOrders();

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var orderList = okResult.Value.Should().BeAssignableTo<List<ElectronicsOrderReadDto>>().Subject;
            orderList.Should().HaveCount(2);

            var order10 = orderList.First(o => o.TotalAmount == 10);
            var order20 = orderList.First(o => o.TotalAmount == 20);

            order10.OrderStatus.Should().Be("PENDING");
            order20.OrderStatus.Should().Be("ACCEPTED");
        }

        [Fact]
        public async Task GetOrderById_WithValidId_ReturnsOrder()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var order = new Models.ElectronicsOrder
            {
                ManufacturerId = 2,
                TotalAmount = 15,
                RemainingAmount = 15,
                OrderedAt = 1.0m,
                OrderStatusId = 1
            };
            context.ElectronicsOrders.Add(order);
            await context.SaveChangesAsync();

            var result = await controller.GetOrderById(order.OrderId);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var orderDto = okResult.Value.Should().BeOfType<ElectronicsOrderReadDto>().Subject;
            orderDto.OrderId.Should().Be(order.OrderId);
            orderDto.TotalAmount.Should().Be(15);
            orderDto.OrderStatus.Should().Be("PENDING");
        }

        [Fact]
        public async Task GetOrderById_WithInvalidId_ReturnsNotFound()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var result = await controller.GetOrderById(999);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateOrder_WithValidData_ReturnsNoContent()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var order = new Models.ElectronicsOrder
            {
                ManufacturerId = 2,
                TotalAmount = 15,
                RemainingAmount = 15,
                OrderedAt = 1.0m,
                OrderStatusId = 1
            };
            context.ElectronicsOrders.Add(order);
            await context.SaveChangesAsync();

            var updateDto = new ElectronicsOrderUpdateDto
            {
                RemainingAmount = 10,
                ProcessedAt = 2.0m,
                OrderStatus = "ACCEPTED"
            };

            var result = await controller.UpdateOrder(order.OrderId, updateDto);

            result.Should().BeOfType<NoContentResult>();

            var updatedOrder = await context.ElectronicsOrders.FindAsync(order.OrderId);
            updatedOrder!.RemainingAmount.Should().Be(10);
            updatedOrder.ProcessedAt.Should().Be(2.0m);
            updatedOrder.OrderStatusId.Should().Be(2);
        }

        [Fact]
        public async Task UpdateOrder_WithInvalidOrderId_ReturnsNotFound()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var updateDto = new ElectronicsOrderUpdateDto { RemainingAmount = 10 };

            var result = await controller.UpdateOrder(999, updateDto);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateOrder_WithInvalidOrderStatus_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var order = new Models.ElectronicsOrder
            {
                ManufacturerId = 2,
                TotalAmount = 15,
                RemainingAmount = 15,
                OrderedAt = 1.0m,
                OrderStatusId = 1
            };
            context.ElectronicsOrders.Add(order);
            await context.SaveChangesAsync();

            var updateDto = new ElectronicsOrderUpdateDto { OrderStatus = "INVALID_STATUS" };

            var result = await controller.UpdateOrder(order.OrderId, updateDto);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Order status 'INVALID_STATUS' not found.");
        }

        [Fact]
        public async Task UpdateOrder_WithNullDto_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            var result = await controller.UpdateOrder(1, null!);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Invalid order data.");
        }

        [Fact]
        public async Task GetInventoryStatus_ReturnsCorrectCounts()
        {
            using var context = CreateContext();
            var controller = CreateController(context);

            _mockOrderExpirationService.Setup(s => s.GetAvailableElectronicsCountAsync())
                .ReturnsAsync(100);
            _mockOrderExpirationService.Setup(s => s.GetReservedElectronicsCountAsync())
                .ReturnsAsync(25);

            var result = await controller.GetInventoryStatus();

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var inventory = okResult.Value.Should().BeAssignableTo<object>().Subject;

            var inventoryType = inventory.GetType();
            var availableProperty = inventoryType.GetProperty("Available");
            var reservedProperty = inventoryType.GetProperty("Reserved");
            var totalProperty = inventoryType.GetProperty("Total");

            availableProperty!.GetValue(inventory).Should().Be(100);
            reservedProperty!.GetValue(inventory).Should().Be(25);
            totalProperty!.GetValue(inventory).Should().Be(125);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task CreateOrder_WithVariousQuantities_CreatesOrderCorrectly(int quantity)
        {
            using var context = CreateContext();
            var manufacturer = context.Companies.First(c => c.CompanyId == 2);
            var controller = CreateController(context, manufacturer);

            var orderRequest = new ElectronicsOrderRequest { Quantity = quantity };

            _mockOrderExpirationService.Setup(s => s.GetAvailableElectronicsCountAsync())
                .ReturnsAsync(200);
            _mockOrderExpirationService.Setup(s => s.ReserveElectronicsForOrderAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            _mockElectronicsService.Setup(s => s.GetElectronicsDetailsAsync())
                .ReturnsAsync(new ElectronicsDetailsDto { AvailableStock = 200, PricePerUnit = 30.00m });

            var result = await controller.CreateOrder(orderRequest);

            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var responseDto = createdResult.Value.Should().BeOfType<ElectronicsOrderResponseDto>().Subject;
            responseDto.Quantity.Should().Be(quantity);
            responseDto.AmountDue.Should().Be(quantity * 30.00m);

            _mockOrderExpirationService.Verify(s => s.ReserveElectronicsForOrderAsync(It.IsAny<int>(), quantity), Times.Once);
        }

        public void Dispose()
        {
            using var context = new AppDbContext(_options);
            context.Database.EnsureDeleted();
        }
    }
}
