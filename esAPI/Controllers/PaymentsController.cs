using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using esAPI.Data;
using esAPI.DTOs;
using esAPI.Models;
using esAPI.Interfaces;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("payments")]
    public class PaymentsController(AppDbContext context, ISimulationStateService simulationStateService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ISimulationStateService _simulationStateService = simulationStateService;

        [HttpPost]
        public async Task<IActionResult> ReceivePayment([FromBody] PaymentNotificationDto dto)
        {
            // Try to find a matching order (stretch: based on description or from account)
            int? matchedOrderId = null;
            if (!string.IsNullOrEmpty(dto.Description))
            {
                // Try to parse order id from description (e.g., "Order #123")
                var orderIdStr = new string(dto.Description.Where(char.IsDigit).ToArray());
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var order = await _context.ElectronicsOrders.FindAsync(orderId);
                    if (order != null)
                    {
                        matchedOrderId = orderId;
                        // If payment is sufficient, set order to ACCEPTED
                        if (dto.Amount >= order.TotalAmount)
                        {
                            var acceptedStatus = await _context.OrderStatuses.FirstOrDefaultAsync(s => s.Status == "ACCEPTED");
                            if (acceptedStatus != null)
                            {
                                order.OrderStatusId = acceptedStatus.StatusId;
                                _context.ElectronicsOrders.Update(order);
                                // Update sold_at for reserved electronics
                                decimal soldAtTime;
                                try
                                {
                                    soldAtTime = (decimal)_simulationStateService.GetCurrentSimulationTime(3);
                                }
                                catch
                                {
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
                TransactionNumber = dto.TransactionNumber,
                Status = dto.Status,
                Amount = dto.Amount,
                Timestamp = dto.Timestamp,
                Description = dto.Description,
                FromAccount = dto.From,
                ToAccount = dto.To,
                OrderId = matchedOrderId
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}