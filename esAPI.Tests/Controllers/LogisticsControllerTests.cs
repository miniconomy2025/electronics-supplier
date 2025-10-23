using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using esAPI.Controllers;
using esAPI.Data;
using esAPI.DTOs.SupplyDtos;
using esAPI.Models;
using esAPI.Models.Enums;
using esAPI.Interfaces;

namespace esAPI.Tests.Controllers
{
    public class LogisticsControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ISimulationStateService> _stateMock;

        public LogisticsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _stateMock = new Mock<ISimulationStateService>();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task HandleLogisticsRequest_InvalidType_ReturnsBadRequest()
        {
            var controller = new LogisticsController(_context, _stateMock.Object);
            var req = new LogisticsRequestDto { Type = "INVALID", Items = new List<LogisticsItemDto> { new LogisticsItemDto { Quantity = 1 } } };

            var result = await controller.HandleLogisticsRequest(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<string>(bad.Value);
        }

        [Fact]
        public async Task HandleLogisticsRequest_Delivery_MachineHappyPath_AddsMachinesAndUpdatesOrder()
        {
            // seed pickup request and machine order
            var pickup = new Models.PickupRequest { RequestId = 123, ExternalRequestId = 1, Type = "PICKUP" };
            var machineOrder = new MachineOrder { OrderId = 1, ExternalOrderId = 1, RemainingAmount = 2, TotalAmount = 2, OrderStatusId = (int)Order.Status.Accepted, SupplierId = 7 };
            _context.PickupRequests.Add(pickup);
            _context.MachineOrders.Add(machineOrder);
            await _context.SaveChangesAsync();

            _stateMock.Setup(s => s.IsRunning).Returns(true);
            _stateMock.Setup(s => s.CurrentDay).Returns(10);

            var controller = new LogisticsController(_context, _stateMock.Object);
            var req = new LogisticsRequestDto { Type = "DELIVERY", Id = 123, Items = new List<LogisticsItemDto> { new LogisticsItemDto { Quantity = 1 } } };

            var result = await controller.HandleLogisticsRequest(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Delivered", ok.Value?.ToString() ?? string.Empty);

            var updatedOrder = await _context.MachineOrders.FindAsync(1);
            Assert.NotNull(updatedOrder);
            Assert.Equal(1, updatedOrder.RemainingAmount);
        }

        [Fact]
        public async Task HandleLogisticsRequest_Delivery_MaterialHappyPath_AddsSuppliesAndUpdatesOrder()
        {
            var pickup = new Models.PickupRequest { RequestId = 200, ExternalRequestId = 2, Type = "PICKUP" };
            var materialOrder = new MaterialOrder { OrderId = 2, ExternalOrderId = 2, RemainingAmount = 3, TotalAmount = 3, OrderStatusId = (int)Order.Status.Accepted, MaterialId = 5 };
            _context.PickupRequests.Add(pickup);
            _context.MaterialOrders.Add(materialOrder);
            await _context.SaveChangesAsync();

            _stateMock.Setup(s => s.IsRunning).Returns(true);
            _stateMock.Setup(s => s.CurrentDay).Returns(20);

            var controller = new LogisticsController(_context, _stateMock.Object);
            var req = new LogisticsRequestDto { Type = "DELIVERY", Id = 200, Items = new List<LogisticsItemDto> { new LogisticsItemDto { Quantity = 2 } } };

            var result = await controller.HandleLogisticsRequest(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("supplies", ok.Value?.ToString() ?? string.Empty);

            var updatedOrder = await _context.MaterialOrders.FindAsync(2);
            Assert.NotNull(updatedOrder);
            Assert.Equal(1, updatedOrder.RemainingAmount);

            var supplies = _context.MaterialSupplies.Where(s => s.MaterialId == 5).ToList();
            Assert.Equal(2, supplies.Count);
        }

        [Fact]
        public async Task HandlePickup_RequestNotFound_ReturnsNotFound()
        {
            var controller = new LogisticsController(_context, _stateMock.Object);
            var req = new LogisticsRequestDto { Type = "PICKUP", Id = 999, Items = new List<LogisticsItemDto> { new LogisticsItemDto { Quantity = 1 } } };

            var result = await controller.HandleLogisticsRequest(req);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No electronics order found", notFound.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task HandlePickup_NotEnoughStock_ReturnsBadRequest()
        {
            // create an order with remaining amount > available stock
            var order = new ElectronicsOrder { OrderId = 300, RemainingAmount = 2, ManufacturerId = 8, OrderStatusId = (int)Order.Status.Accepted };
            _context.ElectronicsOrders.Add(order);
            // only 1 electronic in stock
            _context.Electronics.Add(new Electronic { ProducedAt = 1m, ElectronicsStatusId = 0 });
            await _context.SaveChangesAsync();

            var controller = new LogisticsController(_context, _stateMock.Object);
            var req = new LogisticsRequestDto { Type = "PICKUP", Id = 300, Items = new List<LogisticsItemDto> { new LogisticsItemDto { Quantity = 2 } } };

            var result = await controller.HandleLogisticsRequest(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Not enough electronics stock", bad.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task HandlePickup_SimulationNotRunning_ReturnsBadRequest()
        {
            var order = new ElectronicsOrder { OrderId = 400, RemainingAmount = 1, ManufacturerId = 8, OrderStatusId = (int)Order.Status.Accepted };
            _context.ElectronicsOrders.Add(order);
            _context.Electronics.Add(new Electronic { ProducedAt = 1m, ElectronicsStatusId = 0 });
            await _context.SaveChangesAsync();

            _stateMock.Setup(s => s.IsRunning).Returns(false);

            var controller = new LogisticsController(_context, _stateMock.Object);
            var req = new LogisticsRequestDto { Type = "PICKUP", Id = 400, Items = new List<LogisticsItemDto> { new LogisticsItemDto { Quantity = 1 } } };

            var result = await controller.HandleLogisticsRequest(req);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Simulation not running", bad.Value?.ToString() ?? string.Empty);
        }
    }
}
