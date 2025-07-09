using Microsoft.AspNetCore.Mvc;
using esAPI.Data;
using esAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using esAPI.Services;
using System;
using esAPI.Interfaces;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("payments")]
    public class PaymentsController(AppDbContext context, ISimulationStateService simulationStateService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ISimulationStateService _simulationStateService = simulationStateService;

        public class PaymentNotificationDto
        {
            public string? transaction_number { get; set; }
            public string? status { get; set; }
            public decimal amount { get; set; }
            public double timestamp { get; set; }
            public string? description { get; set; }
            public string? from { get; set; }
            public string? to { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ReceivePayment([FromBody] PaymentNotificationDto dto)
        {
            // Try to find a matching order (stretch: based on description or from account)
            int? matchedOrderId = null;
            if (!string.IsNullOrEmpty(dto.description))
            {
                // Try to parse order id from description (e.g., "Order #123")
                var orderIdStr = new string(dto.description.Where(char.IsDigit).ToArray());
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var order = await _context.ElectronicsOrders.FindAsync(orderId);
                    if (order != null)
                    {
                        matchedOrderId = orderId;
                        // If payment is sufficient, set order to ACCEPTED
                        if (dto.amount >= order.TotalAmount)
                        {
                            var acceptedStatus = await _context.OrderStatuses.FirstOrDefaultAsync(s => s.Status == "ACCEPTED");
                            if (acceptedStatus != null)
                            {
                                order.OrderStatusId = acceptedStatus.StatusId;
                                _context.ElectronicsOrders.Update(order);
                                // Update sold_at for reserved electronics
                                decimal soldAtTime;
                                try {
                                    soldAtTime = (decimal)_simulationStateService.GetCurrentSimulationTime(3);
                                } catch {
                                    soldAtTime = (decimal)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                }
                                var reservedElectronics = await _context.Electronics
                                    .Where(e => e.ElectronicsStatusId == (int)esAPI.Models.Enums.Electronics.Status.Reserved && e.SoldAt == null)
                                    .OrderBy(e => e.ProducedAt)
                                    .Take(order.TotalAmount)
                                    .ToListAsync();
                                foreach (var electronic in reservedElectronics)
                                {
                                    electronic.SoldAt = soldAtTime;
                                }
                                _context.Electronics.UpdateRange(reservedElectronics);
                            }
                        }
                    }
                }
            }

            var payment = new Payment
            {
                TransactionNumber = dto.transaction_number,
                Status = dto.status,
                Amount = dto.amount,
                Timestamp = dto.timestamp,
                Description = dto.description,
                FromAccount = dto.from,
                ToAccount = dto.to,
                OrderId = matchedOrderId
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
} 