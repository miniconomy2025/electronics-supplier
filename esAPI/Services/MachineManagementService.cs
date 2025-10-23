using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Interfaces;
using esAPI.Models;

namespace esAPI.Services
{
    public interface IMachineManagementService
    {
        Task<bool> EnsureMachinesAvailableAsync();
    }

    public class MachineManagementService : IMachineManagementService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<MachineManagementService> _logger;
        private readonly ISimulationStateService _stateService;

        public MachineManagementService(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            ICommercialBankClient bankClient,
            ISimulationStateService stateService,
            ILogger<MachineManagementService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _bankClient = bankClient;
            _stateService = stateService;
            _logger = logger;
        }

        public async Task<bool> EnsureMachinesAvailableAsync()
        {
            try
            {
                int totalMachines = await _context.Machines.CountAsync(m => m.RemovedAt == null);
                int brokenMachines = await _context.Machines.CountAsync(m =>
                    m.MachineStatusId == (int)Models.Enums.Machine.Status.Broken && m.RemovedAt == null);

                // Order machines if we have zero or all are broken
                if (totalMachines == 0 || totalMachines == brokenMachines)
                {
                    _logger.LogInformation($"[Machine] No working machines available. Total: {totalMachines}, Broken: {brokenMachines}. Attempting to buy 2 new machines from THOH.");
                    return await PurchaseMachinesFromThohAsync(2);
                }

                _logger.LogInformation($"[Machine] Machines status OK. Total: {totalMachines}, Broken: {brokenMachines}, Working: {totalMachines - brokenMachines}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine] Error checking machine status or purchasing new machines");
                return false;
            }
        }

        private async Task<bool> PurchaseMachinesFromThohAsync(int quantity)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation($"[Machine] THOH machine order attempt {attempt}/{maxRetries}");
                    var thohHttpClient = _httpClientFactory.CreateClient("thoh");
                    var machineOrderReq = new { machineName = "electronics_machine", quantity };

                    var response = await thohHttpClient.PostAsJsonAsync("api/machines", machineOrderReq);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"[Machine] Failed to order machines from THOH attempt {attempt}. Status: {response.StatusCode}");
                        if (attempt == maxRetries)
                            return false;
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(content);

                    var orderId = doc.RootElement.GetProperty("orderId").GetInt32();
                    var totalPrice = doc.RootElement.GetProperty("totalPrice").GetDecimal();
                    var bankAccount = doc.RootElement.GetProperty("bankAccount").GetString();

                    _logger.LogInformation($"[Machine] Ordered {quantity} new machines from THOH. OrderId={orderId}, TotalPrice={totalPrice}, BankAccount={bankAccount}");

                    // Create machine order record in database
                    await CreateMachineOrderRecordAsync(orderId, quantity);

                    if (!string.IsNullOrEmpty(bankAccount) && totalPrice > 0)
                    {
                        try
                        {
                            await _bankClient.MakePaymentAsync(bankAccount, "thoh", totalPrice, $"Purchase {quantity} electronics_machine from THOH");
                            _logger.LogInformation($"[Machine] Payment sent to THOH for order {orderId}");

                            // Arrange pickup with bulk logistics
                            await ArrangePickupWithBulkLogistics(orderId, quantity);

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"[Machine] Error paying THOH for machine order {orderId}");
                            return false;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"[Machine] Invalid payment details for THOH machine order {orderId}");
                        return false;
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogWarning($"[Machine] Timeout on attempt {attempt}/{maxRetries} for THOH machine purchase");
                    if (attempt == maxRetries)
                    {
                        _logger.LogError("[Machine] All attempts failed due to timeout for THOH machine purchase");
                        return false;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt)); // Quick retry for simulation
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Machine] Exception during machine purchase from THOH attempt {attempt}");
                    if (attempt == maxRetries)
                        return false;
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt)); // Quick retry for simulation
                }
            }

            return false; // All retries failed
        }

        private async Task CreateMachineOrderRecordAsync(int externalOrderId, int quantity)
        {
            try
            {
                // Find THOH company ID
                var thohCompany = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName.ToLower() == "thoh");
                if (thohCompany == null)
                {
                    _logger.LogError("[Machine] THOH company not found in database");
                    return;
                }

                var currentDay = _stateService.GetCurrentSimulationTime(3);

                var machineOrder = new MachineOrder
                {
                    SupplierId = thohCompany.CompanyId,
                    ExternalOrderId = externalOrderId,
                    RemainingAmount = quantity,
                    TotalAmount = quantity,
                    OrderStatusId = 1, // Pending
                    PlacedAt = currentDay
                };

                _context.MachineOrders.Add(machineOrder);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[Machine] Created machine order record: OrderId={machineOrder.OrderId}, ExternalOrderId={externalOrderId}, Quantity={quantity}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine] Error creating machine order record for external order {ExternalOrderId}", externalOrderId);
            }
        }

        private async Task ArrangePickupWithBulkLogistics(int externalOrderId, int quantity)
        {
            try
            {
                _logger.LogInformation("[Machine] Arranging pickup with Bulk Logistics for THOH order {ExternalOrderId}", externalOrderId);

                var bulkClient = _httpClientFactory.CreateClient("bulk-logistics");
                var pickupReq = new
                {
                    originalExternalOrderId = externalOrderId.ToString(),
                    originCompanyId = "thoh",
                    destinationCompanyId = "electronics-supplier",
                    items = new[]
                    {
                        new { name = "electronics_machine", quantity }
                    }
                };

                var pickupResp = await bulkClient.PostAsJsonAsync("/api/pickup-request", pickupReq);

                if (pickupResp.IsSuccessStatusCode)
                {
                    var pickupContent = await pickupResp.Content.ReadAsStringAsync();
                    using var pickupDoc = System.Text.Json.JsonDocument.Parse(pickupContent);
                    var pickup = pickupDoc.RootElement;

                    var pickupRequestId = pickup.GetProperty("pickupRequestId").GetInt32();
                    var cost = pickup.GetProperty("cost").GetDecimal();
                    var bulkBankAccount = pickup.GetProperty("bulkLogisticsBankAccountNumber").GetString();

                    _logger.LogInformation($"[Machine] Pickup arranged: PickupRequestId={pickupRequestId}, Cost={cost}");

                    if (!string.IsNullOrEmpty(bulkBankAccount))
                    {
                        // Pay Bulk Logistics
                        await _bankClient.MakePaymentAsync(bulkBankAccount, "commercial-bank", cost, $"Pickup for THOH machine order {externalOrderId}");
                        _logger.LogInformation($"[Machine] Payment sent to Bulk Logistics for pickup request {pickupRequestId}");

                        // Store pickup request record
                        await CreatePickupRequestRecordAsync(externalOrderId, pickupRequestId, quantity);

                        // Update machine order with pickup request ID
                        await UpdateMachineOrderWithPickupRequestId(externalOrderId, pickupRequestId);
                    }
                }
                else
                {
                    _logger.LogWarning("[Machine] Failed to arrange pickup with Bulk Logistics for THOH order {ExternalOrderId}. Status: {StatusCode}", externalOrderId, pickupResp.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine] Error arranging pickup with Bulk Logistics for THOH order {ExternalOrderId}", externalOrderId);
            }
        }

        private async Task CreatePickupRequestRecordAsync(int externalOrderId, int pickupRequestId, int quantity)
        {
            try
            {
                var pickupRequest = new PickupRequest
                {
                    ExternalRequestId = externalOrderId,
                    PickupRequestId = pickupRequestId,
                    Type = "PICKUP",
                    Quantity = quantity,
                    PlacedAt = (double)_stateService.GetCurrentSimulationTime(3)
                };

                _context.PickupRequests.Add(pickupRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[Machine] Created pickup request record: ExternalOrderId={externalOrderId}, PickupRequestId={pickupRequestId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine] Error creating pickup request record for external order {ExternalOrderId}", externalOrderId);
            }
        }

        private async Task UpdateMachineOrderWithPickupRequestId(int externalOrderId, int pickupRequestId)
        {
            try
            {
                var machineOrder = await _context.MachineOrders
                    .FirstOrDefaultAsync(o => o.ExternalOrderId == externalOrderId);

                if (machineOrder != null)
                {
                    machineOrder.PickupRequestId = pickupRequestId;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"[Machine] Updated machine order {machineOrder.OrderId} with pickup request ID {pickupRequestId}");
                }
                else
                {
                    _logger.LogWarning("[Machine] Could not find machine order with external order ID {ExternalOrderId} to update with pickup request ID {PickupRequestId}", externalOrderId, pickupRequestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine] Error updating machine order with pickup request ID: ExternalOrderId={ExternalOrderId}, PickupRequestId={PickupRequestId}", externalOrderId, pickupRequestId);
            }
        }
    }
}
