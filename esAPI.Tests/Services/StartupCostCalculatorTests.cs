using esAPI.DTOs;
using esAPI.DTOs.Thoh;
using esAPI.Interfaces;
using esAPI.Services;
using FluentAssertions;
using Moq;

namespace esAPI.Tests.Services
{
    public class StartupCostCalculatorUnitTests
    {
        private readonly Mock<IThohApiClient> _mockMachineClient;
        private readonly Mock<ISupplierApiClient> _mockMaterialClient;
        private readonly StartupCostCalculator _calculator;

        public StartupCostCalculatorUnitTests()
        {
            _mockMachineClient = new Mock<IThohApiClient>();
            _mockMaterialClient = new Mock<ISupplierApiClient>();
            _calculator = new StartupCostCalculator(_mockMachineClient.Object, _mockMaterialClient.Object);
        }

        [Fact]
        public async Task GenerateAllPossibleStartupPlansAsync_WithValidMachineAndMaterials_GeneratesCorrectPlan()
        {
            // Arrange
            _mockMachineClient.Setup(c => c.GetAvailableMachinesAsync())
                .ReturnsAsync(new List<ThohMachineDto>
                {
                   new()
                    {
                        MachineName = "ChipMaker 3000",
                        Price = 100000,
                        InputRatio = new Dictionary<string, int>
                        {
                            { "Copper", 2 },
                            { "Silicon", 3 }
                        }
                    }
                });

            _mockMaterialClient.Setup(c => c.GetAvailableMaterialsAsync())
                .ReturnsAsync(new List<SupplierMaterialInfo>
                {
                    new() { MaterialName = "Copper", PricePerKg = 10m },
                    new() { MaterialName = "Silicon", PricePerKg = 20m }
                });

            var plans = await _calculator.GenerateAllPossibleStartupPlansAsync();

            plans.Should().HaveCount(1);
            var plan = plans.First();
            plan.MachineName.Should().Be("ChipMaker 3000");
            plan.MachineCost.Should().Be(100000m);

            // Expected materials cost:
            // InitialProductionCyclesToStock = 2
            // Copper: 2 (ratio) * 2 (cycles) * 10 (price) = 40
            // Silicon: 3 (ratio) * 2 (cycles) * 20 (price) = 120
            // Total: 40 + 120 = 160
            plan.MaterialsCost.Should().Be(160m);
        }

        [Fact]
        public async Task GenerateAllPossibleStartupPlansAsync_WithMachineRequiringUnavailableMaterial_ExcludesPlan()
        {
            // Arrange
            _mockMachineClient.Setup(c => c.GetAvailableMachinesAsync())
               .ReturnsAsync(new List<ThohMachineDto>
               {
                     new()
                    {
                        MachineName = "CopperCoiler",
                        Price = 20000,
                        InputRatio = new Dictionary<string, int> { { "Copper", 5 } }
                    },
                    new()
                    {
                        MachineName = "Gold Plater 500",
                        Price = 50000,
                        InputRatio = new Dictionary<string, int>
                        {
                            { "Gold", 1 },
                            { "Copper", 1 }
                        }
                    }
               });

            _mockMaterialClient.Setup(c => c.GetAvailableMaterialsAsync())
                .ReturnsAsync(new List<SupplierMaterialInfo>
                {
                   new() { MaterialName = "Gold", PricePerKg = 60000m, AvailableQuantity = 100 },
                    new() { MaterialName = "Copper", PricePerKg = 10m, AvailableQuantity = 1000 }
                });

            // Act
            var plans = await _calculator.GenerateAllPossibleStartupPlansAsync();

            // Assert
            plans.Should().HaveCount(1);
            plans.First().MachineName.Should().Be("CopperCoiler");
        }
    }
}
