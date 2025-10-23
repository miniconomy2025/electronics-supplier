using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Interfaces;
using esAPI.Models;
using esAPI.DTOs;

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
        private readonly IThohApiClient _thohApiClient;
        private readonly IBulkLogisticsClient _bulkLogisticsClient;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<MachineManagementService> _logger;
        private readonly ISimulationStateService _stateService;

        public MachineManagementService(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IThohApiClient thohApiClient,
            IBulkLogisticsClient bulkLogisticsClient,
            ICommercialBankClient bankClient,
            ISimulationStateService stateService,
            ILogger<MachineManagementService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _thohApiClient = thohApiClient;
            _bulkLogisticsClient = bulkLogisticsClient;
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

                    // First check if machines are available
                    var availableMachines = await _thohApiClient.GetAvailableMachinesAsync();
                    var electronicsMachine = availableMachines.FirstOrDefault(m => m.MachineName == "electronics_machine");

                    if (electronicsMachine == null || electronicsMachine.Quantity < quantity)
                    {
                        _logger.LogWarning($"[Machine] THOH does not have enough electronics_machine available. Available: {electronicsMachine?.Quantity ?? 0}, Needed: {quantity}");
                        if (attempt == maxRetries)
                            return false;
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                        continue;
                    }

                    // Place machine order with THOH API
                    var thohHttpClient = _httpClientFactory.CreateClient("thoh");
                    var machineOrderReq = new { machineName = "electronics_machine", quantity };

                    var response = await thohHttpClient.PostAsJsonAsync("api/machines", machineOrderReq);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"[Machine] Failed to order machines from THOH attempt {attempt}. Status: {response.StatusCode}");
                        if (attempt == maxRetries)
                            return false;
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
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
                        // Step 1: Make payment to THOH with retry logic
                        bool paymentSuccessful = false;
                        int paymentRetryCount = 0;
                        const int maxPaymentRetries = 3;

                        while (!paymentSuccessful && paymentRetryCount < maxPaymentRetries)
                        {
                            try
                            {
                                await _bankClient.MakePaymentAsync(bankAccount, "thoh", totalPrice,orderId.ToString());
                                _logger.LogInformation($"[Machine] Payment sent to THOH for order {orderId}");
                                paymentSuccessful = true;
                            }
                            catch (Exception ex)
                            {
                                paymentRetryCount++;
                                _logger.LogWarning($"[Machine] Payment attempt {paymentRetryCount}/{maxPaymentRetries} failed for THOH order {orderId}: {ex.Message}");

                                if (paymentRetryCount >= maxPaymentRetries)
                                {
                                    _logger.LogError($"[Machine] Failed to pay THOH after {maxPaymentRetries} attempts for order {orderId}. Skipping logistics arrangement.");
                                    return false;
                                }

                                // Wait before retry (exponential backoff)
                                await Task.Delay(1000 * paymentRetryCount);
                            }
                        }

                        if (!paymentSuccessful)
                        {
                            _logger.LogError($"[Machine] Payment to THOH failed after all retries for order {orderId}");
                            return false;
                        }

                        // Step 2: Arrange pickup with bulk logistics only if payment was successful
                        await ArrangePickupWithBulkLogistics(orderId, quantity);
                        return true;
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

                var pickupRequest = new LogisticsPickupRequest
                {
                    OriginalExternalOrderId = externalOrderId.ToString(),
                    OriginCompany = "thoh",
                    DestinationCompany = "electronics-supplier",
                    Items = [new LogisticsItem { Name = "electronics_machine", Quantity = quantity }]
                };

                var pickupResponse = await _bulkLogisticsClient.ArrangePickupAsync(pickupRequest);

                if (pickupResponse != null)
                {
                    _logger.LogInformation($"[Machine] Pickup arranged: PickupRequestId={pickupResponse.PickupRequestId}, Cost={pickupResponse.Cost}");

                    if (!string.IsNullOrEmpty(pickupResponse.BulkLogisticsBankAccountNumber))
                    {
                        // Pay Bulk Logistics with retry logic
                        bool bulkPaymentSuccessful = false;
                        int bulkRetryCount = 0;
                        const int maxBulkRetries = 3;

                        while (!bulkPaymentSuccessful && bulkRetryCount < maxBulkRetries)
                        {
                            try
                            {
                                await _bankClient.MakePaymentAsync(pickupResponse.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResponse.Cost, $"Pickup for THOH machine order {externalOrderId}");
                                _logger.LogInformation($"[Machine] Payment sent to Bulk Logistics for pickup request {pickupResponse.PickupRequestId}");
                                bulkPaymentSuccessful = true;
                            }
                            catch (Exception ex)
                            {
                                bulkRetryCount++;
                                _logger.LogWarning($"[Machine] Bulk Logistics payment attempt {bulkRetryCount}/{maxBulkRetries} failed for pickup request {pickupResponse.PickupRequestId}: {ex.Message}");

                                if (bulkRetryCount >= maxBulkRetries)
                                {
                                    _logger.LogError($"[Machine] Failed to pay Bulk Logistics after {maxBulkRetries} attempts for pickup request {pickupResponse.PickupRequestId}.");
                                    return; // Don't fail completely, but log the error
                                }

                                // Wait before retry (exponential backoff)
                                await Task.Delay(1000 * bulkRetryCount);
                            }
                        }

                        if (bulkPaymentSuccessful)
                        {
                            // Store pickup request record only if payment was successful
                            await CreatePickupRequestRecordAsync(externalOrderId, pickupResponse.PickupRequestId, quantity);

                            // Update machine order with pickup request ID
                            await UpdateMachineOrderWithPickupRequestId(externalOrderId, pickupResponse.PickupRequestId);
                        }
                        else
                        {
                            _logger.LogError($"[Machine] Bulk Logistics payment failed after all retries for pickup request {pickupResponse.PickupRequestId}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[Machine] Pickup response received but bank account number is null/empty for order {ExternalOrderId}", externalOrderId);
                    }
                }
                else
                {
                    _logger.LogWarning("[Machine] Failed to arrange pickup with Bulk Logistics for THOH order {ExternalOrderId} - response was null", externalOrderId);
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
