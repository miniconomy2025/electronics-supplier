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

namespace esAPI.Tests.Services
{
    public class SimulationDayOrchestratorTests
    {
        [Fact]
        public async Task RunDayAsync_HappyPath_CallsAllServicesInOrder()
        {
            // Arrange
            var mockBankClient = new Mock<ICommercialBankClient>();
            var mockStateService = new Mock<ISimulationStateService>();

            mockBankClient.Setup(b => b.GetAccountBalanceAsync()).ReturnsAsync(1000m);
            mockStateService.Setup(s => s.GetCurrentSimulationTime(It.IsAny<int>())).Returns(123456);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            var dbContext = new AppDbContext(options);

            var bankService = new BankService(dbContext, mockBankClient.Object, mockStateService.Object);

            var mockInventoryService = new Mock<IInventoryService>();
            var mockMachineService = new Mock<IMachineAcquisitionService>();
            var mockMaterialService = new Mock<IMaterialAcquisitionService>();
            var mockProductionService = new Mock<IProductionService>();
            var mockLogger = new Mock<ILogger<SimulationDayOrchestrator>>();

            // Setup inventory: no working machines, no copper, no silicon
            mockInventoryService.Setup(s => s.GetAndStoreInventory())
                .ReturnsAsync(new InventorySummaryDto
                {
                    Machines = new MachineSummaryDto { InUse = 0 },
                    MaterialsInStock = new List<MaterialStockDto>(),
                    ElectronicsInStock = 0
                });

            // Setup machine acquisition
            mockMachineService.Setup(s => s.CheckTHOHForMachines()).ReturnsAsync(true);
            mockMachineService.Setup(s => s.PurchaseMachineViaBank())
                .ReturnsAsync((orderId: 123, quantity: 2));
            mockMachineService.Setup(s => s.QueryOrderDetailsFromTHOH()).Returns(Task.CompletedTask);
            mockMachineService.Setup(s => s.PlaceBulkLogisticsPickup(123, 2)).Returns(Task.CompletedTask);

            // Setup material acquisition
            mockMaterialService.Setup(s => s.ExecutePurchaseStrategyAsync()).Returns(Task.CompletedTask);

            // Setup production
            mockProductionService.Setup(s => s.ProduceElectronics())
                .ReturnsAsync((10, new Dictionary<string, int> { { "copper", 20 }, { "silicon", 10 } }));

            var orchestrator = new SimulationDayOrchestrator(
                bankService,
                mockInventoryService.Object,
                mockMachineService.Object,
                mockMaterialService.Object,
                mockProductionService.Object,
                mockLogger.Object
            );

            // Act
            await orchestrator.RunDayAsync(1);

            // Assert
            mockInventoryService.Verify(s => s.GetAndStoreInventory(), Times.Once);
            mockMachineService.Verify(s => s.CheckTHOHForMachines(), Times.Once);
            mockMachineService.Verify(s => s.PurchaseMachineViaBank(), Times.Once);
            mockMachineService.Verify(s => s.QueryOrderDetailsFromTHOH(), Times.Once);
            mockMachineService.Verify(s => s.PlaceBulkLogisticsPickup(123, 2), Times.Once);
            mockMaterialService.Verify(s => s.ExecutePurchaseStrategyAsync(), Times.Once);
            mockProductionService.Verify(s => s.ProduceElectronics(), Times.Once);
        }
    }
}