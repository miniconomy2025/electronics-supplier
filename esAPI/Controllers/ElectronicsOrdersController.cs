using esAPI.Data;
using esAPI.DTOs.Orders;
using esAPI.Models;
using esAPI.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("orders")]
    public class ElectronicsOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ElectronicsOrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] ElectronicsOrderRequest request)
        {
            if (request == null || request.Quantity <= 0)
                return BadRequest("Invalid order data. Quantity must be greater than 0.");

            // Get current simulation day
            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
            if (sim == null)
                return BadRequest("Simulation not running.");

            // Get current electronics price using a different approach
            var lookupValue = await _context.LookupValues
                .OrderByDescending(lv => lv.ChangedAt)
                .FirstOrDefaultAsync();
            
            if (lookupValue == null)
                return StatusCode(500, "Unable to retrieve current pricing.");
            
            decimal pricePerUnit = lookupValue.ElectronicsPricePerUnit;

            // Get our company's bank account number (electronics-supplier is company_id = 1)
            var ourCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.CompanyId == 1);
            
            if (ourCompany == null)
                return StatusCode(500, "Unable to retrieve company information.");

            // For now, use a default manufacturer ID of 6
            var defaultManufacturerId = 6; // Pear Company

            var order = new Models.ElectronicsOrder
            {
                ManufacturerId = defaultManufacturerId,
                TotalAmount = request.Quantity,
                RemainingAmount = request.Quantity,
                OrderedAt = sim.DayNumber,
                OrderStatusId = (int) Order.Status.Pending // Default to pending
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

            var response = new ElectronicsOrderResponse
            {
                OrderId = order.OrderId,
                Quantity = request.Quantity,
                AmountDue = pricePerUnit * request.Quantity,
                BankNumber = ourCompany.BankAccountNumber ?? "000000000000" // Use company's bank number or default
            };

            return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, response);
        }



        [HttpGet("{orderId}")]
        public async Task<ActionResult<DTOs.Orders.ElectronicsOrder>> GetOrderById(int orderId)
        {
            var order = await _context.ElectronicsOrders
                .Include(o => o.OrderStatus)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var dto = new DTOs.Orders.ElectronicsOrder
            {
                OrderId = order.OrderId,
                Status = order.OrderStatus?.Status ?? "Unknown",
                OrderedAt = order.OrderedAt,
                TotalAmount = order.TotalAmount,
                RemainingAmount = order.RemainingAmount
            };

            return Ok(dto);
        }
    }
}

