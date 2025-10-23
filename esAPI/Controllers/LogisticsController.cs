using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using esAPI.Data;
using esAPI.DTOs.SupplyDtos;
using esAPI.Models;
using esAPI.Models.Enums;

using Machine = esAPI.Models.Machine;
using MS = esAPI.Models.Enums.Machine;
using Electronics = esAPI.Models.Enums.Electronics;
using esAPI.Interfaces;

namespace esAPI.Controllers
{
    [ApiController]
    [Route("logistics")]
    public class LogisticsController(AppDbContext context, ISimulationStateService stateService, ILogger<LogisticsController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ISimulationStateService _stateService = stateService;
        private readonly ILogger<LogisticsController> _logger = logger;

        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandleLogisticsRequest([FromBody] LogisticsRequestDto request)
        {
            try
            {
                // Log the incoming request
                _logger.LogInformation("[Logistics] POST /logistics endpoint called");
                _logger.LogInformation("[Logistics] Request body: {RequestBody}", JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }));
                
                // Validate request
                if (!IsValidRequest(request, out var validationError))
                {
                    _logger.LogWarning("[Logistics] Request validation failed: {ValidationError}", validationError);
                    _logger.LogWarning("[Logistics] Invalid request details - Type: {Type}, Id: {Id}, Items: {ItemCount}", 
                        request.Type, request.Id, request.Items?.Count ?? 0);
                    return BadRequest(validationError);
                }

                _logger.LogInformation("[Logistics] Request validation passed - Type: {Type}, Id: {Id}, Items: {ItemCount}", 
                    request.Type, request.Id, request.Items.Count);

                // Route to appropriate handler
                if (request.Type == "DELIVERY")
                {
                    _logger.LogInformation("[Logistics] Routing to delivery handler for request ID {RequestId}", request.Id);
                    return await HandleDeliveryAsync(request);
                }

                if (request.Type == "PICKUP")
                {
                    _logger.LogInformation("[Logistics] Routing to pickup handler for request ID {RequestId}", request.Id);
                    return await HandlePickupAsync(request);
                }

                _logger.LogWarning("[Logistics] Unhandled request type: {Type} for request ID {RequestId}", request.Type, request.Id);
                return Ok(new { Message = "PICKUP logic not yet implemented." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Logistics] Unhandled exception in HandleLogisticsRequest for request: {RequestBody}", 
                    JsonSerializer.Serialize(request ?? new LogisticsRequestDto(), new JsonSerializerOptions { WriteIndented = true }));
                return StatusCode(500, "An internal error occurred while processing the logistics request");
            }
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
            try
            {
                var pickupReqID = request.Id; // Now directly an int, no parsing needed
                _logger.LogInformation("[Logistics] Starting delivery processing for pickup request ID {PickupRequestId}", pickupReqID);

                if (!_stateService.IsRunning)
                {
                    _logger.LogWarning("[Logistics] Delivery request rejected - simulation not running for pickup request ID {PickupRequestId}", pickupReqID);
                    return BadRequest("Simulation not running.");
                }

                int currentDay = _stateService.CurrentDay;
                _logger.LogInformation("[Logistics] Processing delivery on day {CurrentDay} for pickup request ID {PickupRequestId}", currentDay, pickupReqID);

                // 1. Find the pickup request by ID
                _logger.LogInformation("[Logistics] Looking up pickup request with ID {PickupRequestId}", pickupReqID);
                var pickupRequest = await _context.PickupRequests
                    .FirstOrDefaultAsync(p => p.RequestId == pickupReqID);

                if (pickupRequest == null)
                {
                    _logger.LogWarning("[Logistics] No pickup request found with ID {PickupRequestId}", pickupReqID);
                    return NotFound($"No pickup request found with ID {request.Id}");
                }

                _logger.LogInformation("[Logistics] Found pickup request - ID: {PickupRequestId}, ExternalOrderId: {ExternalOrderId}, Type: {Type}, Quantity: {Quantity}", 
                    pickupRequest.RequestId, pickupRequest.ExternalRequestId, pickupRequest.Type, pickupRequest.Quantity);

                // 2. Determine what type of pickup this is by checking related orders
                _logger.LogInformation("[Logistics] Checking for machine order with external order ID {ExternalOrderId}", pickupRequest.ExternalRequestId);
                var machineOrder = await _context.MachineOrders
                    .FirstOrDefaultAsync(o => o.ExternalOrderId == pickupRequest.ExternalRequestId);
                
                if (machineOrder != null)
                {
                    _logger.LogInformation("[Logistics] Found machine order - ID: {OrderId}, Remaining: {RemainingAmount}, Status: {StatusId}", 
                        machineOrder.OrderId, machineOrder.RemainingAmount, machineOrder.OrderStatusId);
                // 3a. Handle machine delivery
                if (machineOrder.OrderStatusId == (int)Order.Status.Completed)
                    return BadRequest($"Order {request.Id} is already marked as completed.");

                int machineCount = request.Items.Sum(item => item.Quantity);
                int deliverAmount = Math.Min(machineOrder.RemainingAmount, machineCount);

                if (deliverAmount <= 0)
                    return BadRequest("Nothing to deliver based on the remaining amount.");

                var machinesToAdd = Enumerable.Range(0, deliverAmount)
                    .Select(_ => new Machine
                    {
                        OrderId = machineOrder.OrderId,
                        MachineStatusId = (int)MS.Status.Standby,
                        ReceivedAt = currentDay,
                        PurchasedAt = currentDay,
                        PurchasePrice = 0
                    })
                    .ToList();

                _context.Machines.AddRange(machinesToAdd);

                machineOrder.RemainingAmount -= deliverAmount;

                if (machineOrder.RemainingAmount == 0)
                {
                    machineOrder.ReceivedAt = currentDay;
                    machineOrder.OrderStatusId = (int)Order.Status.Completed;
                }
                else if (machineOrder.OrderStatusId == (int)Order.Status.Pending || machineOrder.OrderStatusId == (int)Order.Status.Accepted)
                {
                    machineOrder.OrderStatusId = (int)Order.Status.InProgress;
                }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[Logistics] Successfully delivered {DeliverAmount} machines for supplier ID {SupplierId} from pickup request {RequestId}. Remaining: {Remaining}", 
                        deliverAmount, machineOrder.SupplierId, request.Id, machineOrder.RemainingAmount);

                    return Ok(new
                    {
                        Message = $"Delivered {deliverAmount} machines for supplier ID {machineOrder.SupplierId} from order {request.Id}",
                        Remaining = machineOrder.RemainingAmount
                    });
                }
                else
                {
                    // 3b. Handle material delivery — find matching material order by external order ID
                    _logger.LogInformation("[Logistics] No machine order found, checking for material order with external order ID {ExternalOrderId}", pickupRequest.ExternalRequestId);
                    var materialOrder = await _context.MaterialOrders
                        .FirstOrDefaultAsync(o => o.ExternalOrderId == pickupRequest.ExternalRequestId);

                    if (materialOrder == null)
                    {
                        _logger.LogWarning("[Logistics] No material order found for external order ID {ExternalOrderId} (pickup request {RequestId})", 
                            pickupRequest.ExternalRequestId, request.Id);
                        return NotFound($"No material order found for pickup request {request.Id}");
                    }

                    _logger.LogInformation("[Logistics] Found material order - ID: {OrderId}, MaterialId: {MaterialId}, Remaining: {RemainingAmount}, Status: {StatusId}", 
                        materialOrder.OrderId, materialOrder.MaterialId, materialOrder.RemainingAmount, materialOrder.OrderStatusId);

                    if (materialOrder.OrderStatusId == (int)Order.Status.Completed)
                    {
                        _logger.LogWarning("[Logistics] Material order {OrderId} is already completed (pickup request {RequestId})", 
                            materialOrder.OrderId, request.Id);
                        return BadRequest($"Order {request.Id} is already fully delivered.");
                    }

                // Sum total quantity from request items (in case of multiple items)
                int requestedQuantity = request.Items.Sum(i => i.Quantity);
                int deliverAmount = Math.Min(materialOrder.RemainingAmount, requestedQuantity);

                if (deliverAmount <= 0)
                    return BadRequest("Nothing to deliver based on the remaining amount.");

                var suppliesToAdd = Enumerable.Range(0, deliverAmount)
                    .Select(_ => new MaterialSupply
                    {
                        MaterialId = materialOrder.MaterialId,
                        ReceivedAt = currentDay
                    })
                    .ToList();

                _context.MaterialSupplies.AddRange(suppliesToAdd);

                materialOrder.RemainingAmount -= deliverAmount;

                if (materialOrder.RemainingAmount == 0)
                {
                    materialOrder.ReceivedAt = currentDay;
                    materialOrder.OrderStatusId = (int)Order.Status.Completed;
                }
                else if (materialOrder.OrderStatusId == (int)Order.Status.Pending || materialOrder.OrderStatusId == (int)Order.Status.Accepted)
                {
                    materialOrder.OrderStatusId = (int)Order.Status.InProgress;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("[Logistics] Successfully delivered {DeliverAmount} supplies for material ID {MaterialId} from pickup request {RequestId}. Remaining: {Remaining}", 
                    deliverAmount, materialOrder.MaterialId, request.Id, materialOrder.RemainingAmount);
                
                    return Ok(new
                    {
                        Message = $"Delivered {deliverAmount} supplies for material ID {materialOrder.MaterialId} from order {request.Id}",
                        Remaining = materialOrder.RemainingAmount
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Logistics] Error processing delivery for pickup request ID {PickupRequestId}", request.Id);
                return StatusCode(500, $"An internal error occurred while processing delivery for pickup request {request.Id}");
            }
        }



        private async Task<IActionResult> HandlePickupAsync(LogisticsRequestDto request)
        {
            _logger.LogInformation("[Logistics] Starting pickup processing for electronics order ID {OrderId}", request.Id);
            
            var order = await _context.ElectronicsOrders
                .FirstOrDefaultAsync(o => o.OrderId == request.Id);

            if (order == null)
            {
                _logger.LogWarning("[Logistics] No electronics order found with ID {OrderId} for pickup", request.Id);
                return NotFound($"No electronics order found with ID {request.Id}");
            }

            _logger.LogInformation("[Logistics] Found electronics order - ID: {OrderId}, Remaining: {RemainingAmount}, Status: {StatusId}", 
                order.OrderId, order.RemainingAmount, order.OrderStatusId);

            if (order.RemainingAmount <= 0)
            {
                _logger.LogWarning("[Logistics] Electronics order {OrderId} has no remaining items for pickup", request.Id);
                return BadRequest($"Order {request.Id} is already fully picked up.");
            }

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

            if (!_stateService.IsRunning)
                return BadRequest("Simulation not running.");

            int currentDay = _stateService.CurrentDay;


            foreach (var e in electronicsToRemove)
            {
                e.SoldAt = currentDay;
                e.ElectronicsStatusId = (int)Electronics.Status.Reserved;
            }

            order.RemainingAmount -= pickupAmount;

            if (order.RemainingAmount == 0)
            {
                order.ProcessedAt = currentDay;
                order.OrderStatusId = (int)Order.Status.Completed; // COMPLETED
            }
            else if (order.OrderStatusId == (int)Order.Status.Pending || order.OrderStatusId == (int)Order.Status.Accepted) // PENDING or ACCEPTED
            {
                order.OrderStatusId = (int)Order.Status.InProgress; // IN_PROGRESS
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[Logistics] Successfully picked up {PickupAmount} electronics for manufacturer ID {ManufacturerId} from order {OrderId}. Remaining: {Remaining}", 
                pickupAmount, order.ManufacturerId, request.Id, order.RemainingAmount);

            return Ok(new
            {
                Message = $"Picked up {pickupAmount} electronics for manufacturer ID {order.ManufacturerId} from order {request.Id}",
                Remaining = order.RemainingAmount
            });
        }

    }
}
