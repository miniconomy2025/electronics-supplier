using esAPI.Data;
using esAPI.DTOs.Electronics;
using esAPI.DTOs.Orders;
using esAPI.Services;
using esAPI.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esAPI.Services.ElectronicsSQS;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography.X509Certificates;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("orders")]
    public class ElectronicsOrdersController(
        AppDbContext context,
        IElectronicsService electronicsService,
        ISimulationStateService stateService,
        OrderExpirationService orderExpirationService,
        IElectronicsOrderPublisher orderPublisher,
        ILogger<ElectronicsOrdersController> logger) : BaseController(context)
    {
        private readonly IElectronicsService _electronicsService = electronicsService;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly OrderExpirationService _orderExpirationService = orderExpirationService;
        private readonly IElectronicsOrderPublisher _orderPublisher = orderPublisher;
        private readonly ILogger<ElectronicsOrdersController> _logger = logger;

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] ElectronicsOrderRequest dto)
        {
            // Get current simulation day

            // --- for testing disabled
            // var manufacturer = await GetOrganizationalUnitFromCertificateAsync();
            // if (manufacturer == null)
            //     return Unauthorized("You must be authenticated to place an order.");
            var manufacturer = await _context.Companies
                .Where(c => c.CompanyId == 6) // 6 is pear-company
                .FirstOrDefaultAsync();

            if (manufacturer == null)
                return BadRequest("Manufacturer not found.");

            if (dto == null || dto.Quantity <= 0)
                return BadRequest("Invalid order data.");

            var orderEvent = new ElectronicsOrderReceivedEvent
            {
                CustomerId = manufacturer.CompanyId,
                Amount = dto.Quantity,
                OrderReceivedAtUtc = DateTime.UtcNow
            };



            var availableStock = await _orderExpirationService.GetAvailableElectronicsCountAsync();

            var order = new Models.ElectronicsOrder
            {
                ManufacturerId = manufacturer.CompanyId,
                RemainingAmount = dto.Quantity,
                OrderedAt = _stateService.GetCurrentSimulationTime(3),
                OrderStatusId = 1, //  1 is the ID for "Pending" status
                TotalAmount = dto.Quantity
            };

            bool success = await _orderPublisher.PublishOrderReceivedEventAsync(orderEvent);

            if (success)
            {
                return Accepted(
                    new
                    {
                        Message = "Order received and has been queued for processing. You can check its status via the GET /orders/{id} endpoint later."
                    });
            }
            else
            {
                _logger.LogError("Failed to publish new electronics order event to the queue.");
                return StatusCode(500, new { Error = "The system could not accept your order at this time. Please try again later." });
            }

        }

        [HttpGet]
        public async Task<ActionResult<List<ElectronicsOrderReadDto>>> GetAllOrders()
        {
            var dtoList = await _context.ElectronicsOrders
                .AsNoTracking()
                .Join(
                    _context.OrderStatuses,
                    order => order.OrderStatusId,
                    status => status.StatusId,
                    (order, status) => new ElectronicsOrderReadDto
                    {
                        OrderId = order.OrderId,
                        RemainingAmount = order.RemainingAmount,
                        OrderedAt = order.OrderedAt,
                        ProcessedAt = order.ProcessedAt,
                        OrderStatus = status.Status,
                        TotalAmount = order.TotalAmount
                    }
                )
                .ToListAsync();

            return Ok(dtoList);
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<DTOs.Orders.ElectronicsOrder>> GetOrderById(int orderId)
        {
            var dto = await _context.ElectronicsOrders
                .AsNoTracking()
                .Where(order => order.OrderId == orderId)
                .Join(
                    _context.OrderStatuses,
                    order => order.OrderStatusId,
                    status => status.StatusId,
                    (order, status) => new ElectronicsOrderReadDto
                    {
                        OrderId = order.OrderId,
                        RemainingAmount = order.RemainingAmount,
                        OrderedAt = order.OrderedAt,
                        ProcessedAt = order.ProcessedAt,
                        OrderStatus = status.Status,
                        TotalAmount = order.TotalAmount
                    }
                )
                .FirstOrDefaultAsync();

            if (dto == null)
                return NotFound();

            return Ok(dto);
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] ElectronicsOrderUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid order data.");

            var existingOrder = await _context.ElectronicsOrders.FindAsync(orderId);
            if (existingOrder == null)
                return NotFound();

            if (dto.RemainingAmount.HasValue)
                existingOrder.RemainingAmount = dto.RemainingAmount.Value;

            if (dto.ProcessedAt.HasValue)
                existingOrder.ProcessedAt = dto.ProcessedAt.Value;

            if (!string.IsNullOrWhiteSpace(dto.OrderStatus))
            {
                var statusId = await _context.OrderStatuses
                    .Where(s => s.Status == dto.OrderStatus)
                    .Select(s => s.StatusId)
                    .FirstOrDefaultAsync();
                if (statusId == 0)
                    return BadRequest($"Order status '{dto.OrderStatus}' not found.");
                existingOrder.OrderStatusId = statusId;
            }

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

        [HttpGet("inventory")]
        public async Task<ActionResult<object>> GetInventoryStatus()
        {
            var availableCount = await _orderExpirationService.GetAvailableElectronicsCountAsync();
            var reservedCount = await _orderExpirationService.GetReservedElectronicsCountAsync();
            var totalCount = availableCount + reservedCount;

            return Ok(new
            {
                Available = availableCount,
                Reserved = reservedCount,
                Total = totalCount
            });
        }
    }
}

