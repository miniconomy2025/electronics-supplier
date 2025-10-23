using System.Text.Json;
using esAPI.Clients;
using esAPI.DTOs;
using esAPI.Models;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;

namespace esAPI.Services
{
    public class MaterialAcquisitionService : IMaterialAcquisitionService
    {
        private readonly IMaterialSourcingService _sourcingService;
        private readonly IBulkLogisticsClient _logisticsClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBankService _bankService;
        private readonly ICommercialBankClient _bankClient;
        private readonly AppDbContext _dbContext;
        private readonly ISimulationStateService _stateService;

        private static ConcurrentDictionary<string, int> _statusIdCache = new();

        public MaterialAcquisitionService(IHttpClientFactory httpClientFactory, AppDbContext dbContext, IBankService bankService, ICommercialBankClient bankClient, IMaterialSourcingService sourcingService, IBulkLogisticsClient logisticsClient, ISimulationStateService stateService)
        {
            _httpClientFactory = httpClientFactory;
            _bankService = bankService;
            _bankClient = bankClient;

            _sourcingService = sourcingService;
            _bankClient = bankClient;
            _logisticsClient = logisticsClient;
            _dbContext = dbContext;
            _stateService = stateService;
        }

        public async Task ExecutePurchaseStrategyAsync()
        {
            await PurchaseWithStrategy("copper", sourcedInfo => Task.FromResult(sourcedInfo.MaterialDetails.AvailableQuantity / 2));
            await PurchaseWithStrategy("silicon", async sourcedInfo =>
            {
                var balance = await _bankService.GetAndStoreBalance(_stateService.CurrentDay);
                var budget = balance * 0.3m;
                int qty = (int)Math.Floor(budget / sourcedInfo.MaterialDetails.PricePerKg);
                return Math.Min(qty, sourcedInfo.MaterialDetails.AvailableQuantity);
            });
        }

        private async Task PurchaseWithStrategy(string materialName, Func<SourcedSupplier, Task<int>> quantityStrategy)
        {
            var sourcedInfo = await _sourcingService.FindBestSupplierAsync(materialName);
            Console.WriteLine(sourcedInfo);
            if (sourcedInfo == null) return;

            int quantityToBuy = await quantityStrategy(sourcedInfo);
            Console.WriteLine(quantityToBuy);
            if (quantityToBuy <= 0)
            {
                return;
            }

            await ProcureAndPayAsync(sourcedInfo, quantityToBuy);
        }

        private async Task<bool> ProcureAndPayAsync(SourcedSupplier sourcedInfo, int quantityToBuy)
        {
            var materialName = sourcedInfo.MaterialDetails.MaterialName;

            Console.WriteLine("Try buying " + materialName);

            var orderRequest = new SupplierOrderRequest { MaterialName = materialName, WeightQuantity = quantityToBuy };
            SupplierOrderResponse? orderResponse = null;
            if (sourcedInfo.Client is ISupplierApiClient supplierClient)
            {
                orderResponse = await supplierClient.PlaceOrderAsync(orderRequest);
            }
            else if (sourcedInfo.Client is RecyclerApiClient recyclerClient)
            {
                orderResponse = await recyclerClient.PlaceOrderAsync(orderRequest);
            }
            else if (sourcedInfo.Client is ThohApiClient thohClient)
            {
                // If ThohApiClient supports PlaceOrderAsync, call it here. Otherwise, log or throw.
                // orderResponse = await thohClient.PlaceOrderAsync(orderRequest);
                Console.WriteLine("PlaceOrderAsync not implemented for ThohApiClient");
                return false;
            }

            if (orderResponse == null || string.IsNullOrEmpty(orderResponse.BankAccount)) return false;

            var localOrder = await CreateLocalOrderRecordAsync(sourcedInfo, quantityToBuy, orderResponse);
            if (localOrder == null)
            {
                return false;
            }

            // --- Attempt to Pay the Supplier ---
            var paymentSuccess = await _bankClient.MakePaymentAsync(orderResponse.BankAccount, sourcedInfo.Name, orderResponse.Price, orderResponse.OrderId.ToString());
            if (paymentSuccess == string.Empty)
            {
                await UpdateOrderStatusAsync(localOrder.OrderId, "REJECTED");
                return false;
            }

            // --- Update Order Status to 'ACCEPTED' ---
            await UpdateOrderStatusAsync(localOrder.OrderId, "ACCEPTED");

            // --- Arrange and Pay for Logistics ---
            var logisticsSuccess = await ArrangeLogisticsAsync(orderResponse, materialName, quantityToBuy, sourcedInfo.Name);

            if (!logisticsSuccess)
            {
                await UpdateOrderStatusAsync(localOrder.OrderId, "DISASTER");
                return false;
            }

            // --- Final Status Update to 'IN_TRANSIT' ---
            await UpdateOrderStatusAsync(localOrder.OrderId, "IN_TRANSIT");

            return true;
        }

        private async Task<bool> ArrangeLogisticsAsync(SupplierOrderResponse order, string materialName, int quantity, string supplierName)
        {
            Console.WriteLine($"[MaterialAcquisition] Arranging pickup for material: '{materialName}' (quantity: {quantity}) from {supplierName}");

            var pickupReq = new LogisticsPickupRequest
            {
                OriginalExternalOrderId = order.OrderId.ToString(),
                OriginCompany = supplierName,
                DestinationCompany = "electronics-supplier",
                Items = [new LogisticsItem { Name = materialName, Quantity = quantity }]
            };

            var pickupResp = await _logisticsClient.ArrangePickupAsync(pickupReq);
            if (pickupResp == null || string.IsNullOrEmpty(pickupResp.BulkLogisticsBankAccountNumber)) return false;

            var pickupRequest = await CreatePickupRequestAsync(order.OrderId, pickupResp.PickupRequestId, quantity, materialName);

            if (pickupRequest == null)
                return false;

            // Update the material order with the pickup request ID
            await UpdateMaterialOrderWithPickupRequestId(order.OrderId, pickupResp.PickupRequestId);

            var paymentSuccess = await _bankClient.MakePaymentAsync(pickupResp.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResp.Cost, pickupResp.PickupRequestId.ToString());

            return paymentSuccess == string.Empty;
        }

        private async Task<MaterialOrder?> CreateLocalOrderRecordAsync(SourcedSupplier sourcedInfo, int quantity, SupplierOrderResponse supplierResponse)
        {
            int pendingStatusId = await GetStatusIdAsync("PENDING");
            if (pendingStatusId == 0) return null;

            var supplierCompanyId = (await _dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyName == sourcedInfo.Name))?.CompanyId;
            var materialId = (await _dbContext.Materials.FirstOrDefaultAsync(m => m.MaterialName == sourcedInfo.MaterialDetails.MaterialName))?.MaterialId;

            if (supplierCompanyId == null || materialId == null)
            {
                return null;
            }

            var newOrder = new MaterialOrder
            {
                SupplierId = supplierCompanyId.Value,
                MaterialId = materialId.Value,
                ExternalOrderId = supplierResponse.OrderId,
                RemainingAmount = quantity,
                TotalAmount = quantity,
                OrderStatusId = pendingStatusId,
                OrderedAt = 1.0m,
            };

            _dbContext.MaterialOrders.Add(newOrder);
            await _dbContext.SaveChangesAsync();

            return newOrder;
        }

        private async Task UpdateOrderStatusAsync(int orderId, string newStatusName)
        {
            var order = await _dbContext.MaterialOrders.FindAsync(orderId);
            int newStatusId = await GetStatusIdAsync(newStatusName);

            if (order != null && newStatusId != 0)
            {
                order.OrderStatusId = newStatusId;
                await _dbContext.SaveChangesAsync();
            }
            else
            {

            }
        }

        private async Task<int> GetStatusIdAsync(string statusName)
        {
            if (_statusIdCache.TryGetValue(statusName, out int cachedId))
            {
                return cachedId;
            }

            var status = await _dbContext.OrderStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Status == statusName);
            if (status != null)
            {
                _statusIdCache.TryAdd(statusName, status.StatusId);
                return status.StatusId;
            }

            return 0;
        }

        private async Task<PickupRequest?> CreatePickupRequestAsync(int externalOrderId, int pickupRequestId, int quantity, string materialName)
        {
            var pickupRequest = new PickupRequest
            {
                ExternalRequestId = externalOrderId,
                PickupRequestId = pickupRequestId,
                Type = "PICKUP", // Operation type for Bulk Logistics API
                Quantity = quantity,
                PlacedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            _dbContext.PickupRequests.Add(pickupRequest);
            await _dbContext.SaveChangesAsync();

            return pickupRequest;
        }

        private async Task UpdateMaterialOrderWithPickupRequestId(int externalOrderId, int pickupRequestId)
        {
            try
            {
                var materialOrder = await _dbContext.MaterialOrders
                    .FirstOrDefaultAsync(o => o.ExternalOrderId == externalOrderId);

                if (materialOrder != null)
                {
                    materialOrder.PickupRequestId = pickupRequestId;
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"[DB] Updated material order {materialOrder.OrderId} with pickup request ID {pickupRequestId}");
                }
                else
                {
                    Console.WriteLine($"⚠️ [DB] Could not find material order with external order ID {externalOrderId} to update with pickup request ID {pickupRequestId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [DB] Error updating material order with pickup request ID: ExternalOrderId={externalOrderId}, PickupRequestId={pickupRequestId}");
                Console.WriteLine($"❌ [DB] Material order update exception details: {ex.Message}");
            }
        }

    }
}
