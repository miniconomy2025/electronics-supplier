using System.Collections.Generic;
using System.Threading.Tasks;
using esAPI.DTOs;
using esAPI.DTOs.Electronics;
using esAPI.Services;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace esAPI.Tests.Services
{
    public class SimulationDayOrchestratorTests
    {
        [Fact]
        public async Task RunDayAsync_HappyPath_CallsAllServicesInOrder()
        {
            // Arrange
            var mockBankService = new Mock<BankService>(null, null, null);
            var mockInventoryService = new Mock<InventoryService>(null);
            var mockMachineService = new Mock<MachineAcquisitionService>(null, null, null);
            var mockMaterialService = new Mock<MaterialAcquisitionService>(null, null, null);
            var mockProductionService = new Mock<ProductionService>(null);
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
            mockMaterialService.Setup(s => s.PurchaseMaterialsViaBank()).Returns(Task.CompletedTask);

            // Setup production
            mockProductionService.Setup(s => s.ProduceElectronics())
                .ReturnsAsync((10, new Dictionary<string, int> { { "copper", 20 }, { "silicon", 10 } }));

            var orchestrator = new SimulationDayOrchestrator(
                mockBankService.Object,
                mockInventoryService.Object,
                mockMachineService.Object,
                mockMaterialService.Object,
                mockProductionService.Object,
                mockLogger.Object
            );

            // Act
            await orchestrator.RunDayAsync(1);

            // Assert
            mockBankService.Verify(s => s.GetAndStoreBalance(1), Times.Once);
            mockInventoryService.Verify(s => s.GetAndStoreInventory(), Times.Once);
            mockMachineService.Verify(s => s.CheckTHOHForMachines(), Times.Once);
            mockMachineService.Verify(s => s.PurchaseMachineViaBank(), Times.Once);
            mockMachineService.Verify(s => s.QueryOrderDetailsFromTHOH(), Times.Once);
            mockMachineService.Verify(s => s.PlaceBulkLogisticsPickup(123, 2), Times.Once);
            mockMaterialService.Verify(s => s.PurchaseMaterialsViaBank(), Times.Once);
            mockProductionService.Verify(s => s.ProduceElectronics(), Times.Once);
        }
    }
} 