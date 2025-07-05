using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Dtos;
using esAPI.Models;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogisticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LogisticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandleLogisticsRequest([FromBody] LogisticsRequestDto request)
        {
            if (!IsValidRequest(request, out var validationError))
                return BadRequest(validationError);

            if (request.Type == "DELIVERY")
                return await HandleDeliveryAsync(request);

            if (request.Type == "PICKUP")
                return await HandlePickupAsync(request);

            return Ok(new { Message = "PICKUP logic not yet implemented." });
        }

        private static bool IsValidRequest(LogisticsRequestDto request, out string error)
        {
            if (request.Type != "PICKUP" && request.Type != "DELIVERY")
            {
                error = "Type must be either 'PICKUP' or 'DELIVERY'";
                return false;
            }

            if (request.Quantity <= 0)
            {
                error = "Quantity must be greater than 0";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private async Task<IActionResult> HandleDeliveryAsync(LogisticsRequestDto request)
        {
            if (!int.TryParse(request.Id, out var externalOrderId))
                return BadRequest("Invalid external order ID format.");
                
            var order = await _context.MaterialOrders
                .FirstOrDefaultAsync(o => o.ExternalOrderId == externalOrderId);

            if (order == null)
                return NotFound($"No material order found with ID {request.Id}");

            if (order.RemainingAmount <= 0)
                return BadRequest($"Order {request.Id} is already fully delivered.");

            int deliverAmount = Math.Min(order.RemainingAmount, request.Quantity);

            if (deliverAmount <= 0)
                return BadRequest("Nothing to deliver based on the remaining amount.");

            var now = DateTime.UtcNow;

            var suppliesToAdd = Enumerable.Range(0, deliverAmount)
                .Select(_ => new Supply
                {
                    MaterialId = order.MaterialId,
                    ReceivedAt = now
                })
                .ToList();

            _context.Supplies.AddRange(suppliesToAdd);

            order.RemainingAmount -= deliverAmount;

            if (order.RemainingAmount == 0)
                order.ReceivedAt = now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Delivered {deliverAmount} supplies for material ID {order.MaterialId} from order {request.Id}",
                Remaining = order.RemainingAmount
            });
        }

        private async Task<IActionResult> HandlePickupAsync(LogisticsRequestDto request)
        {
            var order = await _context.ElectronicsOrders
                .FirstOrDefaultAsync(o => o.OrderId.ToString() == request.Id);

            if (order == null)
                return NotFound($"No electronics order found with ID {request.Id}");

            if (order.RemainingAmount <= 0)
                return BadRequest($"Order {request.Id} is already fully picked up.");

            int pickupAmount = Math.Min(order.RemainingAmount, request.Quantity);

            if (pickupAmount <= 0)
                return BadRequest("Nothing to pick up based on the remaining amount.");

            var electronicsToRemove = await _context.Electronics
                .Where(e => e.SoldAt == null)
                .OrderBy(e => e.ProducedAt)
                .Take(pickupAmount)
                .ToListAsync();

            if (electronicsToRemove.Count < pickupAmount)
                return BadRequest("Not enough electronics stock available to fulfill the pickup.");

            var now = DateTime.UtcNow;
            foreach (var e in electronicsToRemove)
            {
                e.SoldAt = now;
            }

            order.RemainingAmount -= pickupAmount;

            if (order.RemainingAmount == 0)
                order.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Picked up {pickupAmount} electronics for manufacturer ID {order.ManufacturerId} from order {request.Id}",
                Remaining = order.RemainingAmount
            });
        }

    }
}
