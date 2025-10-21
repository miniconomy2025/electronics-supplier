using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using esAPI.Controllers;
using esAPI.DTOs.Supply;
using esAPI.Interfaces;

namespace esAPI.Tests.Controllers
{
    public class SuppliesControllerTests
    {
        [Fact]
        public async Task CreateSupply_ReturnsCreatedAtAction_OnSuccess()
        {
            var mockService = new Mock<ISupplyService>();
            var dto = new CreateSupplyDto { MaterialId = 1, ReceivedAt = 12345m };
            var created = new SupplyDto { SupplyId = 99, MaterialId = 1, ReceivedAt = 12345m };
            mockService.Setup(s => s.CreateSupplyAsync(dto)).ReturnsAsync(created);

            var controller = new SuppliesController(mockService.Object);

            var result = await controller.CreateSupply(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(SuppliesController.GetSupplyById), createdResult.ActionName);
            Assert.NotNull(createdResult.Value);
            var returnedDto = Assert.IsType<SupplyDto>(createdResult.Value);
            Assert.Equal(99, returnedDto.SupplyId);
        }

        [Fact]
        public async Task CreateSupply_ReturnsNotFound_OnKeyNotFound()
        {
            var mockService = new Mock<ISupplyService>();
            var dto = new CreateSupplyDto { MaterialId = 999, ReceivedAt = 12345m };
            mockService.Setup(s => s.CreateSupplyAsync(dto)).ThrowsAsync(new KeyNotFoundException("Not found"));

            var controller = new SuppliesController(mockService.Object);

            var result = await controller.CreateSupply(dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
            Assert.Equal("Not found", notFound.Value);
        }

        [Fact]
        public async Task GetAllSupplies_ReturnsOk_WithList()
        {
            var mockService = new Mock<ISupplyService>();
            var list = new List<SupplyDto> { new SupplyDto { SupplyId = 1, MaterialId = 2, ReceivedAt = 111 } };
            mockService.Setup(s => s.GetAllSuppliesAsync()).ReturnsAsync(list);

            var controller = new SuppliesController(mockService.Object);

            var result = await controller.GetAllSupplies();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<SupplyDto>>(ok.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetSupplyById_ReturnsOk_WhenExists()
        {
            var mockService = new Mock<ISupplyService>();
            var dto = new SupplyDto { SupplyId = 5, MaterialId = 3, ReceivedAt = 111 };
            mockService.Setup(s => s.GetSupplyByIdAsync(5)).ReturnsAsync(dto);

            var controller = new SuppliesController(mockService.Object);

            var result = await controller.GetSupplyById(5);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<SupplyDto>(ok.Value);
            Assert.Equal(5, returned.SupplyId);
        }

        [Fact]
        public async Task GetSupplyById_ReturnsNotFound_WhenMissing()
        {
            var mockService = new Mock<ISupplyService>();
            mockService.Setup(s => s.GetSupplyByIdAsync(10)).ReturnsAsync((SupplyDto?)null);

            var controller = new SuppliesController(mockService.Object);

            var result = await controller.GetSupplyById(10);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteSupplyById_ReturnsNoContent_OnSuccess()
        {
            var mockService = new Mock<ISupplyService>();
            mockService.Setup(s => s.DeleteSupplyByIdAsync(3)).ReturnsAsync(true);

            var controller = new SuppliesController(mockService.Object);

            var result = await controller.DeleteSupplyById(3);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteSupplyById_ReturnsNotFound_WhenMissing()
        {
            var mockService = new Mock<ISupplyService>();
            mockService.Setup(s => s.DeleteSupplyByIdAsync(999)).ReturnsAsync(false);

            var controller = new SuppliesController(mockService.Object);

            var result = await controller.DeleteSupplyById(999);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
            Assert.Contains("Supply with ID 999", notFound.Value.ToString());
        }
    }
}
