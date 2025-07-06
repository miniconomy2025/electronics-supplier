using esAPI.Data;
using esAPI.DTOs.Electronics;
using esAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("electronics/orders")]
    public class ElectronicsOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ElectronicsOrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] ElectronicsOrderCreateDto dto)
        {
            if (dto == null || dto.ManufacturerId <= 0 || dto.RemainingAmount <= 0)
                return BadRequest("Invalid order data.");

            // Get current simulation day
            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
            if (sim == null)
                return BadRequest("Simulation not running.");

            var order = new ElectronicsOrder
            {
                ManufacturerId = dto.ManufacturerId,
                RemainingAmount = dto.RemainingAmount,
                OrderedAt = sim.DayNumber
            };
            _context.ElectronicsOrders.Add(order);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "An error occurred while saving the order.");
            }

            var readDto = new ElectronicsOrderReadDto
            {
                OrderId = order.OrderId,
                ManufacturerId = order.ManufacturerId,
                RemainingAmount = order.RemainingAmount,
                OrderedAt = order.OrderedAt,
                ProcessedAt = order.ProcessedAt
            };

            return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, readDto);
        }

        [HttpGet]
        public async Task<ActionResult<List<ElectronicsOrderReadDto>>> GetAllOrders()
        {
            var orders = await _context.ElectronicsOrders.ToListAsync();

            var dtoList = orders.Select(order => new ElectronicsOrderReadDto
            {
                OrderId = order.OrderId,
                ManufacturerId = order.ManufacturerId,
                RemainingAmount = order.RemainingAmount,
                OrderedAt = order.OrderedAt,
                ProcessedAt = order.ProcessedAt
            }).ToList();

            return Ok(dtoList);
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<ElectronicsOrderReadDto>> GetOrderById(int orderId)
        {
            var order = await _context.ElectronicsOrders.FindAsync(orderId);

            if (order == null)
                return NotFound();

            var dto = new ElectronicsOrderReadDto
            {
                OrderId = order.OrderId,
                ManufacturerId = order.ManufacturerId,
                RemainingAmount = order.RemainingAmount,
                OrderedAt = order.OrderedAt,
                ProcessedAt = order.ProcessedAt
            };

            return Ok(dto);
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] ElectronicsOrderUpdateDto dto)
        {
            if (dto == null || dto.ManufacturerId <= 0 || dto.RemainingAmount <= 0)
                return BadRequest("Invalid order data.");

            var existingOrder = await _context.ElectronicsOrders.FindAsync(orderId);
            if (existingOrder == null)
                return NotFound();

            // Get current simulation day
            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
            if (sim == null)
                return BadRequest("Simulation not running.");

            existingOrder.ManufacturerId = (int)dto.ManufacturerId;
            existingOrder.RemainingAmount = (int)dto.RemainingAmount;
            existingOrder.ProcessedAt = sim.DayNumber;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "An error occurred while updating the order.");
            }

            return NoContent();
        }

        [HttpDelete("{orderId}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var order = await _context.ElectronicsOrders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            _context.ElectronicsOrders.Remove(order);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "An error occurred while deleting the order.");
            }

            return NoContent();
        }
    }
}

