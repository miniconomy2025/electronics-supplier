using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Clients;
using esAPI.Interfaces;
using esAPI.DTOs;
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
                    _logger.LogWarning($"[Order] Failed to place THOH order for {materialName}");
                    return false;
                }

                _logger.LogInformation($"[Order] THOH order placed: OrderId={thohOrderResp.OrderId}, Total={thohOrderResp.Price}, Account={thohOrderResp.BankAccount}");

                // Store order in database
                await CreateMaterialOrderRecordAsync("thoh", materialName, thohQty, thohOrderResp.OrderId, dayNumber);

                // Arrange logistics
                await ArrangeLogisticsAsync(thohOrderResp.OrderId.ToString(), "thoh", materialName, thohQty);

                // Make payment
                if (thohOrderResp.Price > 0)
                {
                    _logger.LogInformation($"[Payment] Paying THOH {thohOrderResp.Price} for order {thohOrderResp.OrderId}");
                    await _bankClient.MakePaymentAsync(thohOrderResp.BankAccount, "thoh", thohOrderResp.Price, thohOrderResp.OrderId.ToString());
                    _logger.LogInformation($"[Payment] Payment sent for THOH order {thohOrderResp.OrderId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Order] Exception during THOH order for {materialName}. Will attempt Recycler fallback.");
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

                int orderQty = mat.AvailableQuantity / 2;
                if (orderQty == 0)
                {
                    _logger.LogInformation($"[Order] Recycler available quantity for {materialName} is zero after division. Skipping order.");
                    return false;
                }

                _logger.LogInformation($"[Order] Placing recycler order for {orderQty} kg of {mat.MaterialName} (our stock: {ownStock})");

                var orderResponse = await _recyclerClient.PlaceRecyclerOrderAsync(mat.MaterialName, orderQty);

                if (orderResponse?.IsSuccess != true || orderResponse.Data == null)
                {
                    _logger.LogWarning($"[Order] Failed to place recycler order for {mat.MaterialName}");
                    return false;
                }

                var orderId = orderResponse.Data.OrderId;
                var total = orderResponse.Data.Total;
                var accountNumber = orderResponse.Data.AccountNumber;

                _logger.LogInformation($"[Order] Recycler order placed: OrderId={orderId}, Total={total}, Account={accountNumber}");

                // Store order in database
                await CreateMaterialOrderRecordAsync("recycler", mat.MaterialName, orderQty, orderId, dayNumber);

                // Arrange logistics
                await ArrangeLogisticsAsync(orderId.ToString(), "recycler", mat.MaterialName.ToLower(), orderQty);

                // Make payment
                if (!string.IsNullOrEmpty(accountNumber) && total > 0)
                {
                    _logger.LogInformation($"[Payment] Paying recycler {total} for order {orderId}");
                    await _bankClient.MakePaymentAsync(accountNumber, "commercial-bank", total, orderId.ToString());
                    _logger.LogInformation($"[Payment] Payment sent for recycler order {orderId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Order] Exception during Recycler order for {materialName}");
                return false;
            }
        }

        private async Task CreateMaterialOrderRecordAsync(string supplierName, string materialName, int quantity, int orderId, int dayNumber)
        {
            try
            {
                var supplier = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName.ToLower() == supplierName);
                var material = await _context.Materials.FirstOrDefaultAsync(m => m.MaterialName.ToLower() == materialName.ToLower());

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
                    _logger.LogWarning($"[DB] Could not insert material order for {supplierName}: missing company or material. Material={materialName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DB] Exception inserting material order for {supplierName}: Material={materialName}");
            }
        }

        private async Task ArrangeLogisticsAsync(string externalOrderId, string originCompany, string materialName, int quantity)
        {
            try
            {
                var pickupRequest = new LogisticsPickupRequest
                {
                    OriginalExternalOrderId = externalOrderId,
                    OriginCompany = originCompany,
                    DestinationCompany = "electronics-supplier",
                    Items = new[] { new LogisticsItem { Name = materialName, Quantity = quantity } }
                };

                var pickupResponse = await _bulkLogisticsClient.ArrangePickupAsync(pickupRequest);

                if (pickupResponse != null && !string.IsNullOrEmpty(pickupResponse.BulkLogisticsBankAccountNumber))
                {
                    _logger.LogInformation($"[Logistics] Pickup arranged. Paying {pickupResponse.Cost} to Bulk Logistics.");

                    try
                    {
                        await _bankClient.MakePaymentAsync(pickupResponse.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResponse.Cost, $"Pickup for {originCompany} order {externalOrderId}");
                        _logger.LogInformation($"[Logistics] Payment sent to Bulk Logistics for pickup of order {externalOrderId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[Logistics] Error paying Bulk Logistics for pickup of order {externalOrderId}");
                    }

                    // Insert pickup request record
                    await CreatePickupRequestRecordAsync(int.Parse(externalOrderId), materialName, quantity);
                }
                else
                {
                    _logger.LogWarning($"[Logistics] Failed to arrange pickup with Bulk Logistics for order {externalOrderId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Logistics] Exception during Bulk Logistics integration for order {externalOrderId}");
            }
        }

        private async Task CreatePickupRequestRecordAsync(int externalOrderId, string materialName, int quantity)
        {
            try
            {
                var pickupType = materialName.ToLower() == "copper"
                    ? Models.Enums.PickupRequest.PickupType.COPPER
                    : Models.Enums.PickupRequest.PickupType.SILICONE;

                var pickupDb = new PickupRequest
                {
                    ExternalRequestId = externalOrderId,
                    Type = pickupType,
                    Quantity = quantity,
                    PlacedAt = (double)_simulationStateService.GetCurrentSimulationTime(3)
                };

                _context.PickupRequests.Add(pickupDb);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[DB] Inserted pickup request for Bulk Logistics: OrderId={externalOrderId}, Material={materialName}, Qty={quantity}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DB] Error inserting pickup request for Bulk Logistics: OrderId={externalOrderId}, Material={materialName}");
            }
        }
    }
}
