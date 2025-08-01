using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using esAPI.Controllers;
using esAPI.Interfaces;
using esAPI.DTOs;
using esAPI.Models;
using esAPI.Data;

namespace esAPI.Tests.Controllers
{
    public class MachineControllerTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<ISimulationStateService> _mockStateService;

        public MachineControllerTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockStateService = new Mock<ISimulationStateService>();
            _mockStateService.Setup(s => s.GetCurrentSimulationTime(3)).Returns(1.500m);
        }

        private AppDbContext CreateContext()
        {
            var context = new AppDbContext(_options);

            // Seed machine statuses
            if (!context.Set<MachineStatus>().Any())
            {
                context.Set<MachineStatus>().AddRange(
                    new MachineStatus { StatusId = 1, Status = "STANDBY" },
                    new MachineStatus { StatusId = 2, Status = "IN_USE" },
                    new MachineStatus { StatusId = 3, Status = "BROKEN" }
                );
                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public async Task ReportMachineFailure_WithValidRequest_ShouldBreakMachinesAndRecordDisaster()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new MachinesController(context, _mockStateService.Object);

            // Add some working machines
            var standbyStatus = context.Set<MachineStatus>().First(s => s.Status == "STANDBY");
            var inUseStatus = context.Set<MachineStatus>().First(s => s.Status == "IN_USE");

            var machines = new List<Machine>
            {
                new Machine { MachineStatusId = standbyStatus.StatusId, PurchasePrice = 1000f, PurchasedAt = 1.0m },
                new Machine { MachineStatusId = inUseStatus.StatusId, PurchasePrice = 1500f, PurchasedAt = 1.1m },
                new Machine { MachineStatusId = standbyStatus.StatusId, PurchasePrice = 2000f, PurchasedAt = 1.2m },
                new Machine { MachineStatusId = inUseStatus.StatusId, PurchasePrice = 2500f, PurchasedAt = 1.3m }
            };

            context.Machines.AddRange(machines);
            await context.SaveChangesAsync();

            var failureRequest = new MachineFailureDto
            {
                MachineName = "electronics_machine",
                FailureQuantity = 3,
                SimulationDate = "2050-01-15",
                SimulationTime = "14:30:45"
            };

            // Act
            var result = await controller.ReportMachineFailure(failureRequest);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var disasterDto = createdResult.Value.Should().BeOfType<DisasterDto>().Subject;

            disasterDto.MachinesAffected.Should().Be(3);
            disasterDto.BrokenAt.Should().Be(1.500m);

            // Verify machines were broken
            var brokenMachines = await context.Machines
                .Where(m => m.MachineStatusId == 3) // BROKEN status
                .CountAsync();
            brokenMachines.Should().Be(3);

            // Verify disaster was recorded
            var disasters = await context.Disasters.ToListAsync();
            disasters.Should().HaveCount(1);
            disasters[0].MachinesAffected.Should().Be(3);
            disasters[0].BrokenAt.Should().Be(1.500m);
        }

        [Fact]
        public async Task ReportMachineFailure_WithMoreMachinesThanAvailable_ShouldBreakAllAvailable()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new MachinesController(context, _mockStateService.Object);

            // Add only 2 working machines
            var standbyStatus = context.Set<MachineStatus>().First(s => s.Status == "STANDBY");
            var machines = new List<Machine>
            {
                new Machine { MachineStatusId = standbyStatus.StatusId, PurchasePrice = 1000f, PurchasedAt = 1.0m },
                new Machine { MachineStatusId = standbyStatus.StatusId, PurchasePrice = 1500f, PurchasedAt = 1.1m }
            };

            context.Machines.AddRange(machines);
            await context.SaveChangesAsync();

            var failureRequest = new MachineFailureDto
            {
                MachineName = "electronics_machine",
                FailureQuantity = 5, // Request more than available
                SimulationDate = "2050-01-15",
                SimulationTime = "14:30:45"
            };

            // Act
            var result = await controller.ReportMachineFailure(failureRequest);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var disasterDto = createdResult.Value.Should().BeOfType<DisasterDto>().Subject;

            disasterDto.MachinesAffected.Should().Be(2); // Only 2 machines were available

            // Verify all machines were broken
            var brokenMachines = await context.Machines
                .Where(m => m.MachineStatusId == 3) // BROKEN status
                .CountAsync();
            brokenMachines.Should().Be(2);
        }

        [Fact]
        public async Task ReportMachineFailure_WithNoWorkingMachines_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new MachinesController(context, _mockStateService.Object);

            // Add only broken machines
            var brokenStatus = context.Set<MachineStatus>().First(s => s.Status == "BROKEN");
            var machines = new List<Machine>
            {
                new Machine { MachineStatusId = brokenStatus.StatusId, PurchasePrice = 1000f, PurchasedAt = 1.0m },
                new Machine { MachineStatusId = brokenStatus.StatusId, PurchasePrice = 1500f, PurchasedAt = 1.1m }
            };

            context.Machines.AddRange(machines);
            await context.SaveChangesAsync();

            var failureRequest = new MachineFailureDto
            {
                MachineName = "electronics_machine",
                FailureQuantity = 3,
                SimulationDate = "2050-01-15",
                SimulationTime = "14:30:45"
            };

            // Act
            var result = await controller.ReportMachineFailure(failureRequest);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("No working machines available to break.");
        }

        [Fact]
        public async Task ReportMachineFailure_WithInvalidModel_ShouldReturnBadRequest()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new MachinesController(context, _mockStateService.Object);

            var failureRequest = new MachineFailureDto
            {
                MachineName = "", // Invalid - empty name
                FailureQuantity = 0, // Invalid - zero quantity
                SimulationDate = "2050-01-15",
                SimulationTime = "14:30:45"
            };

            // Act
            var result = await controller.ReportMachineFailure(failureRequest);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task ReportMachineFailure_ShouldPrioritizeInUseMachinesFirst()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new MachinesController(context, _mockStateService.Object);

            var standbyStatus = context.Set<MachineStatus>().First(s => s.Status == "STANDBY");
            var inUseStatus = context.Set<MachineStatus>().First(s => s.Status == "IN_USE");

            // Add machines in specific order
            var machines = new List<Machine>
            {
                new Machine { MachineId = 1, MachineStatusId = standbyStatus.StatusId, PurchasePrice = 1000f, PurchasedAt = 1.0m },
                new Machine { MachineId = 2, MachineStatusId = inUseStatus.StatusId, PurchasePrice = 1500f, PurchasedAt = 1.1m },
                new Machine { MachineId = 3, MachineStatusId = standbyStatus.StatusId, PurchasePrice = 2000f, PurchasedAt = 1.2m }
            };

            context.Machines.AddRange(machines);
            await context.SaveChangesAsync();

            var failureRequest = new MachineFailureDto
            {
                MachineName = "electronics_machine",
                FailureQuantity = 2,
                SimulationDate = "2050-01-15",
                SimulationTime = "14:30:45"
            };

            // Act
            var result = await controller.ReportMachineFailure(failureRequest);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var disasterDto = createdResult.Value.Should().BeOfType<DisasterDto>().Subject;

            disasterDto.MachinesAffected.Should().Be(2);

            // Verify the first two machines (by ID) were broken
            var brokenMachines = await context.Machines
                .Where(m => m.MachineStatusId == 3) // BROKEN status
                .OrderBy(m => m.MachineId)
                .ToListAsync();

            brokenMachines.Should().HaveCount(2);
            brokenMachines[0].MachineId.Should().Be(1);
            brokenMachines[1].MachineId.Should().Be(2);
        }
    }
}