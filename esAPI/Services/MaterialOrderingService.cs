using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Interfaces;
using esAPI.DTOs;
using esAPI.Logging;
using esAPI.Models;

namespace esAPI.Services
{
    public interface IMaterialOrderingService
    {
        Task<bool> OrderMaterialIfLowStockAsync(string materialName, int ownStock, int dayNumber);
    }

    public class MaterialOrderingService : IMaterialOrderingService
    {
        private readonly AppDbContext _context;
        private readonly IThohApiClient _thohApiClient;
        private readonly IRecyclerApiClient _recyclerClient;
        private readonly ICommercialBankClient _bankClient;
        private readonly IBulkLogisticsClient _bulkLogisticsClient;
        private readonly ISimulationStateService _simulationStateService;
        private readonly ILogger<MaterialOrderingService> _logger;

        public MaterialOrderingService(
            AppDbContext context,
            IThohApiClient thohApiClient,
            IRecyclerApiClient recyclerClient,
            ICommercialBankClient bankClient,
            IBulkLogisticsClient bulkLogisticsClient,
            ISimulationStateService simulationStateService,
            ILogger<MaterialOrderingService> logger)
        {
            _context = context;
            _thohApiClient = thohApiClient;
            _recyclerClient = recyclerClient;
            _bankClient = bankClient;
            _bulkLogisticsClient = bulkLogisticsClient;
            _simulationStateService = simulationStateService;
            _logger = logger;
        }

        public async Task<bool> OrderMaterialIfLowStockAsync(string materialName, int ownStock, int dayNumber)
        {
            if (ownStock >= 1000)
            {
                _logger.LogInformation($"[Order] {materialName} stock is sufficient ({ownStock}), no order needed");
                return true;
            }

            // Try THOH first
            if (await TryOrderFromThohAsync(materialName, ownStock, dayNumber))
            {
                return true;
            }

            // Fallback to Recycler
            return await TryOrderFromRecyclerAsync(materialName, ownStock, dayNumber);
        }

        private async Task<bool> TryOrderFromThohAsync(string materialName, int ownStock, int dayNumber)
        {
            try
            {
                var thohMaterials = await _thohApiClient.GetAvailableMaterialsAsync();
                var thohMat = thohMaterials?.FirstOrDefault(m => m.MaterialName.ToLower() == materialName);

                if (thohMat == null || thohMat.AvailableQuantity <= 0)
                {
                    _logger.LogInformation($"[Order] THOH has no available {materialName} or zero quantity. Will attempt Recycler fallback.");
                    return false;
                }

                int thohQty = thohMat.AvailableQuantity / 2;
                if (thohQty <= 0)
                {
                    _logger.LogInformation($"[Order] THOH quantity for {materialName} is zero after division. Will attempt Recycler fallback.");
                    return false;
                }

                _logger.LogInformation($"[Order] Attempting THOH order for {thohQty} kg of {materialName} (our stock: {ownStock})");

                var thohOrderReq = new SupplierOrderRequest { MaterialName = materialName, WeightQuantity = thohQty };
                var thohOrderResp = await _thohApiClient.PlaceOrderAsync(thohOrderReq);

                if (thohOrderResp == null || string.IsNullOrEmpty(thohOrderResp.BankAccount))
                {
                    _logger.LogWarningColored("[Order] Failed to place THOH order for {0}", materialName);
                    return false;
                }

                _logger.LogInformation($"[Order] THOH order placed: OrderId={thohOrderResp.OrderId}, Total={thohOrderResp.Price}, Account={thohOrderResp.BankAccount}");

                // Store order in database
                await CreateMaterialOrderRecordAsync("thoh", materialName, thohQty, thohOrderResp.OrderId, dayNumber);

                // Step 1: Make payment to supplier first with retry logic
                if (thohOrderResp.Price > 0)
                {
                    _logger.LogInformation($"[Payment] Paying THOH {thohOrderResp.Price} for order {thohOrderResp.OrderId}");
                    
                    bool paymentSuccessful = false;
                    int retryCount = 0;
                    const int maxRetries = 3;
                    
                    while (!paymentSuccessful && retryCount < maxRetries)
                    {
                        try
                        {
                            await _bankClient.MakePaymentAsync(thohOrderResp.BankAccount, "thoh", thohOrderResp.Price, thohOrderResp.OrderId.ToString());
                            _logger.LogInformation($"[Payment] Payment sent to THOH for order {thohOrderResp.OrderId}");
                            paymentSuccessful = true;
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            _logger.LogWarningColored("[Payment] Payment attempt {0}/{1} failed for THOH order {2}: {3}", retryCount, maxRetries, thohOrderResp.OrderId, ex.Message);
                            
                            if (retryCount >= maxRetries)
                            {
                                _logger.LogErrorColored("[Payment] Failed to pay THOH after {0} attempts for order {1}. Skipping logistics arrangement.", maxRetries, thohOrderResp.OrderId);
                                return false;
                            }
                            
                            // Wait before retry (exponential backoff)
                            await Task.Delay(1000 * retryCount);
                        }
                    }
                    
                    if (!paymentSuccessful)
                    {
                        _logger.LogErrorColored("[Payment] Payment to THOH failed after all retries for order {0}", thohOrderResp.OrderId);
                        return false;
                    }
                }

                // Step 2: Arrange logistics and pay Bulk Logistics only if payment was successful
                await ArrangeLogisticsAsync(thohOrderResp.OrderId.ToString(), "thoh", materialName, thohQty);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored("[Order] Exception during THOH order for {0}. Will attempt Recycler fallback.", materialName);
                _logger.LogError(ex, "[Order] THOH order exception details: {Message}", ex.Message);
                return false;
            }
        }

        private async Task<bool> TryOrderFromRecyclerAsync(string materialName, int ownStock, int dayNumber)
        {
            try
            {
                _logger.LogInformation($"[Order] Attempting Recycler fallback for {materialName}.");

                var recyclerMaterials = await _recyclerClient.GetAvailableMaterialsAsync();
                var mat = recyclerMaterials.FirstOrDefault(m => m.MaterialName.ToLower() == materialName);

                if (mat == null || mat.AvailableQuantity <= 0)
                {
                    _logger.LogInformation($"[Order] No order placed for {materialName} (our stock: {ownStock}, recycler available: 0)");
                    return false;
                }

                // Calculate order quantity ensuring it's a multiple of 1000 kg (Recycler requirement)
                int desiredQty = mat.AvailableQuantity / 2;
                int orderQty = desiredQty / 1000 * 1000; // Round down to nearest 1000
                
                if (orderQty == 0)
                {
                    _logger.LogInformation($"[Order] Recycler available quantity for {materialName} ({mat.AvailableQuantity} kg) results in order size ({desiredQty} kg) below minimum 1000 kg requirement. Skipping order.");
                    return false;
                }

                _logger.LogInformation($"[Order] Placing recycler order for {orderQty} kg of {mat.MaterialName} (available: {mat.AvailableQuantity} kg, desired: {desiredQty} kg, rounded to 1000kg multiple: {orderQty} kg, our stock: {ownStock})");

                var orderResponse = await _recyclerClient.PlaceRecyclerOrderAsync(mat.MaterialName, orderQty);

                if (orderResponse?.IsSuccess != true || orderResponse.Data == null)
                {
                    _logger.LogWarningColored("[Order] Failed to place recycler order for {0}", mat.MaterialName);
                    return false;
                }

                var orderId = orderResponse.Data.OrderId;
                var total = orderResponse.Data.Total;
                var accountNumber = orderResponse.Data.AccountNumber;

                _logger.LogInformation($"[Order] Recycler order placed: OrderId={orderId}, Total={total}, Account={accountNumber}");

                // Store order in database
                await CreateMaterialOrderRecordAsync("recycler", mat.MaterialName, orderQty, orderId, dayNumber);

                // Step 1: Make payment to supplier first with retry logic
                if (!string.IsNullOrEmpty(accountNumber) && total > 0)
                {
                    _logger.LogInformation($"[Payment] Paying Recycler {total} for order {orderId}");
                    
                    bool paymentSuccessful = false;
                    int retryCount = 0;
                    const int maxRetries = 3;
                    
                    while (!paymentSuccessful && retryCount < maxRetries)
                    {
                        try
                        {
                            await _bankClient.MakePaymentAsync(accountNumber, "commercial-bank", total, orderId.ToString());
                            _logger.LogInformation($"[Payment] Payment sent to Recycler for order {orderId}");
                            paymentSuccessful = true;
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            _logger.LogWarningColored("[Payment] Payment attempt {0}/{1} failed for Recycler order {2}: {3}", retryCount, maxRetries, orderId, ex.Message);
                            
                            if (retryCount >= maxRetries)
                            {
                                _logger.LogErrorColored("[Payment] Failed to pay Recycler after {0} attempts for order {1}. Skipping logistics arrangement.", maxRetries, orderId);
                                return false;
                            }
                            
                            // Wait before retry (exponential backoff)
                            await Task.Delay(1000 * retryCount);
                        }
                    }
                    
                    if (!paymentSuccessful)
                    {
                        _logger.LogErrorColored("[Payment] Payment to Recycler failed after all retries for order {0}", orderId);
                        return false;
                    }
                }

                // Step 2: Arrange logistics and pay Bulk Logistics only if payment was successful
                await ArrangeLogisticsAsync(orderId.ToString(), "recycler", mat.MaterialName.ToLower(), orderQty);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored("[Order] Exception during Recycler order for {0}", materialName);
                _logger.LogError(ex, "[Order] Recycler order exception details: {Message}", ex.Message);
                return false;
            }
        }

        private async Task CreateMaterialOrderRecordAsync(string supplierName, string materialName, int quantity, int orderId, int dayNumber)
        {
            try
            {
                var supplier = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName.ToLower() == supplierName);
                var material = await _context.Materials.FirstOrDefaultAsync(m => m.MaterialName.ToLower() == materialName.ToLower());

                // Auto-create missing material if it doesn't exist
                if (supplier != null && material == null)
                {
                    _logger.LogInformation($"[DB] Creating missing material: {materialName}");
                    material = new Material
                    {
                        MaterialName = materialName.ToLower(),
                        PricePerKg = 10.0m // Default price
                    };
                    _context.Materials.Add(material);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"[DB] Created material: {materialName} with ID {material.MaterialId}");
                }

                if (supplier != null && material != null)
                {
                    var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
                    var orderedAt = sim?.DayNumber ?? dayNumber;

                    var newOrder = new MaterialOrder
                    {
                        SupplierId = supplier.CompanyId,
                        MaterialId = material.MaterialId,
                        ExternalOrderId = orderId,
                        RemainingAmount = quantity,
                        TotalAmount = quantity,
                        OrderStatusId = 1, // Pending
                        OrderedAt = orderedAt,
                    };

                    _context.MaterialOrders.Add(newOrder);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"[DB] Inserted material order for {supplierName}: Material={materialName}, Qty={quantity}, OrderId={orderId}");
                }
                else
                {
                    var supplierExists = supplier != null ? "found" : "NOT FOUND";
                    var materialExists = material != null ? "found" : "NOT FOUND";
                    _logger.LogWarningColored("[DB] Could not insert material order for {0}: Supplier={1}, Material={2} {3}", supplierName, supplierExists, materialName, materialExists);
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored("[DB] Exception inserting material order for {0}: Material={1}", supplierName, materialName);
                _logger.LogError(ex, "[DB] Material order exception details: {Message}", ex.Message);
            }
        }

        private async Task ArrangeLogisticsAsync(string externalOrderId, string originCompany, string materialName, int quantity)
        {
            try
            {
                _logger.LogInformation("[MaterialOrdering] Arranging pickup for material: '{MaterialName}' (quantity: {Quantity}) from {OriginCompany}", materialName, quantity, originCompany);
                
                var pickupRequest = new LogisticsPickupRequest
                {
                    OriginalExternalOrderId = externalOrderId,
                    OriginCompany = originCompany,
                    DestinationCompany = "electronics-supplier",
                    Items = [new LogisticsItem { Name = materialName, Quantity = quantity }]
                };

                var pickupResponse = await _bulkLogisticsClient.ArrangePickupAsync(pickupRequest);

                if (pickupResponse != null)
                {
                    _logger.LogInformation($"[Logistics] Pickup response received: ID={pickupResponse.PickupRequestId}, Cost={pickupResponse.Cost}, BankAccount='{pickupResponse.BulkLogisticsBankAccountNumber}', Status='{pickupResponse.Status}'");
                    
                    if (!string.IsNullOrEmpty(pickupResponse.BulkLogisticsBankAccountNumber))
                    {
                        _logger.LogInformation($"[Payment] Paying Bulk Logistics {pickupResponse.Cost} for pickup service (Order: {externalOrderId})");

                        try
                        {
                            await _bankClient.MakePaymentAsync(pickupResponse.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResponse.Cost, pickupResponse.PickupRequestId.ToString());
                            _logger.LogInformation($"[Payment] Payment sent to Bulk Logistics for pickup request ID {pickupResponse.PickupRequestId}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogErrorColored("[Payment] Error paying Bulk Logistics for pickup of order {0}", externalOrderId);
                            _logger.LogError(ex, "[Payment] Bulk Logistics payment exception details: {Message}", ex.Message);
                        }

                        // Insert pickup request record
                        await CreatePickupRequestRecordAsync(int.Parse(externalOrderId), pickupResponse.PickupRequestId, materialName, quantity);
                    }
                    else
                    {
                        _logger.LogWarningColored("[Logistics] Pickup response received but bank account number is null/empty for order {0}", externalOrderId);
                    }
                }
                else
                {
                    _logger.LogWarningColored("[Logistics] Failed to arrange pickup with Bulk Logistics for order {0} - response was null", externalOrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored("[Logistics] Exception during Bulk Logistics integration for order {0}", externalOrderId);
                _logger.LogError(ex, "[Logistics] Bulk Logistics exception details: {Message}", ex.Message);
            }
        }

        private async Task CreatePickupRequestRecordAsync(int externalOrderId, int pickupRequestId, string materialName, int quantity)
        {
            try
            {
                // Map material names to pickup types using string constants
                var pickupType = materialName.ToLower() switch
                {
                    "copper" => "COPPER",
                    "silicon" or "silicone" => "SILICONE", 
                    "machine" => "MACHINE",
                    _ => "SILICONE" // Default fallback
                };

                var pickupDb = new PickupRequest
                {
                    ExternalRequestId = externalOrderId,
                    PickupRequestId = pickupRequestId,
                    Type = pickupType,
                    Quantity = quantity,
                    PlacedAt = (double)_simulationStateService.GetCurrentSimulationTime(3)
                };

                _context.PickupRequests.Add(pickupDb);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[DB] Inserted pickup request for Bulk Logistics: ExternalOrderId={externalOrderId}, PickupRequestId={pickupRequestId}, Material={materialName}, Type={pickupType}, Qty={quantity}");
            }
            catch (Exception ex)
            {
                _logger.LogErrorColored("[DB] Error inserting pickup request for Bulk Logistics: ExternalOrderId={0}, PickupRequestId={1}, Material={2}", externalOrderId, pickupRequestId, materialName);
                _logger.LogError(ex, "[DB] Pickup request exception details: {Message}", ex.Message);
            }
        }
    }
}
