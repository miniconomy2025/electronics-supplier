using System.Text.Json;
using esAPI.Clients;
using esAPI.DTOs;
using esAPI.Models;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using esAPI.Interfaces;

namespace esAPI.Services
{
    public interface IMaterialAcquisitionService
    {
        Task ExecutePurchaseStrategyAsync();
        // Task PurchaseMaterialsViaBank();
        // Task PlaceBulkLogisticsPickup(int orderId, string itemName, int quantity, string supplier);
    }

    public class MaterialAcquisitionService : IMaterialAcquisitionService
    {
        private readonly IMaterialSourcingService _sourcingService;
        private readonly IBulkLogisticsClient _logisticsClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BankService _bankService;
        private readonly ICommercialBankClient _bankClient;
        private readonly AppDbContext _dbContext;

        private static ConcurrentDictionary<string, int> _statusIdCache = new();

        public MaterialAcquisitionService(IHttpClientFactory httpClientFactory, AppDbContext dbContext, BankService bankService, ICommercialBankClient bankClient, IMaterialSourcingService sourcingService, IBulkLogisticsClient logisticsClient)
        {
            _httpClientFactory = httpClientFactory;
            _bankService = bankService;
            _bankClient = bankClient;

            _sourcingService = sourcingService;
            _bankClient = bankClient;
            _logisticsClient = logisticsClient;
            _dbContext = dbContext;
        }

        public async Task ExecutePurchaseStrategyAsync()
        {
            await PurchaseWithStrategy("copper", sourcedInfo => Task.FromResult(sourcedInfo.MaterialDetails.AvailableQuantity / 2));
            await PurchaseWithStrategy("silicon", async sourcedInfo =>
            {
                var balance = await _bankService.GetAndStoreBalance(0);
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
            var orderResponse = await sourcedInfo.Client.PlaceOrderAsync(orderRequest);
            if (orderResponse == null || string.IsNullOrEmpty(orderResponse.BankAccount)) return false;

            var localOrder = await CreateLocalOrderRecordAsync(sourcedInfo, quantityToBuy, orderResponse);
            if (localOrder == null)
            {
                return false;
            }

            // --- Attempt to Pay the Supplier ---
            var paymentSuccess = await _bankClient.MakePaymentAsync(orderResponse.BankAccount, sourcedInfo.Name, orderResponse.Price, $"Order {orderResponse.OrderId}");
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
            var pickupReq = new LogisticsPickupRequest
            {
                OriginalExternalOrderId = order.OrderId.ToString(),
                OriginCompanyId = supplierName,
                DestinationCompanyId = "1",
                Items = [new LogisticsItem { Name = materialName, Quantity = quantity }]
            };

            var pickupResp = await _logisticsClient.ArrangePickupAsync(pickupReq);
            if (pickupResp == null || string.IsNullOrEmpty(pickupResp.BulkLogisticsBankAccountNumber)) return false;

            var pickupRequest = await CreatePickupRequestAsync(order.OrderId, quantity, materialName);

            if (pickupRequest == null)
                return false;

            var paymentSuccess = await _bankClient.MakePaymentAsync(pickupResp.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResp.Cost, $"Pickup for order {order.OrderId}");

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

        private async Task<PickupRequest?> CreatePickupRequestAsync(int externalOrderId, int quantity, string materialName)
        {
            Models.Enums.PickupRequest.PickupType pickupType;

            // Map the materialName string to PickupType enum
            if (materialName.Equals("copper", StringComparison.OrdinalIgnoreCase))
                pickupType = Models.Enums.PickupRequest.PickupType.COPPER;
            else if (materialName.Equals("silicone", StringComparison.OrdinalIgnoreCase))
                pickupType = Models.Enums.PickupRequest.PickupType.SILICONE;
            else
                throw new ArgumentException($"Unsupported material name: {materialName}");

            var pickupRequest = new PickupRequest
            {
                ExternalRequestId = externalOrderId,
                Type = pickupType,
                Quantity = quantity,
                PlacedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            _dbContext.PickupRequests.Add(pickupRequest);
            await _dbContext.SaveChangesAsync();

            return pickupRequest;
        }


    }
}