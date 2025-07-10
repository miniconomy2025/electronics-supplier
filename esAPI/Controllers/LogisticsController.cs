using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Dtos;
using esAPI.Models;
using esAPI.Models.Enums;

using Machine = esAPI.Models.Machine;
using MS = esAPI.Models.Enums.Machine;
using Electronics = esAPI.Models.Enums.Electronics;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("logistics")]
    public class LogisticsController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

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
                error = "Type must be either 'PICKUP', 'DELIVERY' or 'MACHINE'";
                return false;
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                error = "At least one item must be specified.";
                return false;
            }

            if (request.Items[0].Quantity <= 0)
            {
                error = "Quantity must be greater than 0.";
                return false;
            }


            error = string.Empty;
            return true;
        }

        private async Task<IActionResult> HandleDeliveryAsync(LogisticsRequestDto request)
        {
            if (!int.TryParse(request.Id, out var pickupReqID))
                return BadRequest("Invalid external order ID format.");

            bool isMachineDelivery = request.Items.All(item => item.Name.Equals("machine", StringComparison.OrdinalIgnoreCase));

            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
            if (sim == null)
                return BadRequest("Simulation not running.");
                
            if (isMachineDelivery)
            {
                // Machine delivery
                var machineOrder = await _context.MachineOrders
                    .FirstOrDefaultAsync(o => o.PickupRequestId  == pickupReqID); 

                if (machineOrder == null)
                    return NotFound($"No machine order found with ID {request.Id}");

                if (machineOrder.OrderStatusId == (int)Order.Status.Completed)
                    return BadRequest($"Order {request.Id} is already marked as completed.");

                int machineCount = request.Items.Count;
                int deliverAmount = Math.Min(machineOrder.RemainingAmount, machineCount);

                if (deliverAmount <= 0)
                    return BadRequest("Nothing to deliver based on the remaining amount.");

                var machinesToAdd = Enumerable.Range(0, deliverAmount)
                    .Select(_ => new Machine
                    {
                        OrderId = machineOrder.OrderId,
                        MachineStatusId = (int)MS.Status.Standby,
                        ReceivedAt = sim.DayNumber,
                        PurchasedAt = sim.DayNumber,
                        PurchasePrice = 0
                    })
                    .ToList();

                _context.Machines.AddRange(machinesToAdd);

                machineOrder.RemainingAmount -= deliverAmount;

                if (machineOrder.RemainingAmount == 0)
                {
                    machineOrder.ReceivedAt = sim.DayNumber;
                    machineOrder.OrderStatusId = (int)Order.Status.Completed;
                }
                else if (machineOrder.OrderStatusId == (int)Order.Status.Pending || machineOrder.OrderStatusId == (int)Order.Status.Accepted)
                {
                    machineOrder.OrderStatusId = (int)Order.Status.InProgress;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"Delivered {deliverAmount} machines for supplier ID {machineOrder.SupplierId} from order {request.Id}",
                    Remaining = machineOrder.RemainingAmount
                });
            }
            else
            {
                // MATERIAL DELIVERY
                var order = await _context.MaterialOrders
                    .FirstOrDefaultAsync(o => o.PickupRequestId == pickupReqID);

                if (order == null)
                    return NotFound($"No material order found with ID {request.Id}");

                if (order.OrderStatusId == (int)Order.Status.Completed)
                    return BadRequest($"Order {request.Id} is already fully delivered.");

                int deliverAmount = Math.Min(order.RemainingAmount, request.Items[0].Quantity);

                if (deliverAmount <= 0)
                    return BadRequest("Nothing to deliver based on the remaining amount.");

                var suppliesToAdd = Enumerable.Range(0, deliverAmount)
                    .Select(_ => new MaterialSupply
                    {
                        MaterialId = order.MaterialId,
                        ReceivedAt = sim.DayNumber
                    })
                    .ToList();

                _context.MaterialSupplies.AddRange(suppliesToAdd);

                order.RemainingAmount -= deliverAmount;

                if (order.RemainingAmount == 0)
                {
                    order.ReceivedAt = sim.DayNumber;
                    order.OrderStatusId = (int)Order.Status.Completed;
                }
                else if (order.OrderStatusId == (int)Order.Status.Pending || order.OrderStatusId == (int)Order.Status.Accepted)
                {
                    order.OrderStatusId = (int)Order.Status.InProgress;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = $"Delivered {deliverAmount} supplies for material ID {order.MaterialId} from order {request.Id}",
                    Remaining = order.RemainingAmount
                });
            }
        }


        private async Task<IActionResult> HandlePickupAsync(LogisticsRequestDto request)
        {
            var order = await _context.ElectronicsOrders
                .FirstOrDefaultAsync(o => o.OrderId.ToString() == request.Id);

            if (order == null)
                return NotFound($"No electronics order found with ID {request.Id}");

            if (order.RemainingAmount <= 0)
                return BadRequest($"Order {request.Id} is already fully picked up.");

            int pickupAmount = Math.Min(order.RemainingAmount, request.Items[0].Quantity);

            if (pickupAmount <= 0)
                return BadRequest("Nothing to pick up based on the remaining amount.");

            var electronicsToRemove = await _context.Electronics
                .Where(e => e.SoldAt == null)
                .OrderBy(e => e.ProducedAt)
                .Take(pickupAmount)
                .ToListAsync();

            if (electronicsToRemove.Count < pickupAmount)
                return BadRequest("Not enough electronics stock available to fulfill the pickup.");

            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
            if (sim == null)
                return BadRequest("Simulation not running.");

            foreach (var e in electronicsToRemove)
            {
                e.SoldAt = sim.DayNumber;
                e.ElectronicsStatusId = (int) Electronics.Status.Reserved;
            }

            order.RemainingAmount -= pickupAmount;

            if (order.RemainingAmount == 0)
            {
                order.ProcessedAt = sim.DayNumber;
                order.OrderStatusId = (int) Order.Status.Completed; // COMPLETED
            }
            else if (order.OrderStatusId == (int) Order.Status.Pending || order.OrderStatusId == (int) Order.Status.Accepted) // PENDING or ACCEPTED
            {
                order.OrderStatusId = (int) Order.Status.InProgress; // IN_PROGRESS
            }

                await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Picked up {pickupAmount} electronics for manufacturer ID {order.ManufacturerId} from order {request.Id}",
                Remaining = order.RemainingAmount
            });
        }

    }
}
