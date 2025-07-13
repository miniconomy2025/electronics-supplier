using System.Text.Json;
using esAPI.Clients;
using esAPI.DTOs;
using esAPI.Models;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using esAPI.Interfaces;
using esAPI.Exceptions;

namespace esAPI.Services
{
    public interface IMaterialAcquisitionService
    {
        Task ExecutePurchaseStrategyAsync();

    }

    public class MaterialAcquisitionService : IMaterialAcquisitionService
    {
        private readonly IMaterialSourcingService _sourcingService;
        private readonly IBulkLogisticsClient _logisticsClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BankService _bankService;
        private readonly ICommercialBankClient _bankClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<MaterialAcquisitionService> _logger;

        private static ConcurrentDictionary<string, int> _statusIdCache = new();

        private const string OurCompanyId = "electronics-supplier";
        private const string LogisticsProviderName = "bulk-logistics";

        public MaterialAcquisitionService(
            IHttpClientFactory httpClientFactory,
            AppDbContext dbContext,
            BankService bankService,
            ICommercialBankClient bankClient,
            IMaterialSourcingService sourcingService,
            IBulkLogisticsClient logisticsClient,
            ILogger<MaterialAcquisitionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _bankService = bankService;
            _bankClient = bankClient;

            _sourcingService = sourcingService;
            _logisticsClient = logisticsClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ExecutePurchaseStrategyAsync()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "A critical error occurred during the material purchase strategy execution.");
            }

        }

        private async Task PurchaseWithStrategy(string materialName, Func<SourcedSupplier, Task<int>> quantityStrategy)
        {
            var sourcedInfo = await _sourcingService.FindBestSupplierAsync(materialName);
            if (sourcedInfo == null)
            {
                _logger.LogInformation("No available supplier for {Material}. Skipping purchase.", materialName);
                return;
            }

            int quantityToBuy = await quantityStrategy(sourcedInfo);
            Console.WriteLine(quantityToBuy);
            if (quantityToBuy <= 0)
            {
                _logger.LogInformation("Strategy for {Material} resulted in zero quantity. Skipping purchase.", materialName);
                return;
            }
            await ProcureMaterialWorkflowAsync(sourcedInfo, quantityToBuy);
        }

        private async Task ProcureMaterialWorkflowAsync(SourcedSupplier sourcedInfo, int quantityToBuy)
        {
            MaterialOrder? localOrder = null;
            try
            {
                var supplierOrderResponse = await PlaceSupplierOrderAsync(sourcedInfo, quantityToBuy);
                localOrder = await CreateLocalOrderRecordAsync(sourcedInfo, quantityToBuy, supplierOrderResponse);

                await PaySupplierAsync(localOrder, supplierOrderResponse, sourcedInfo.Name);
                await UpdateOrderStatusAsync(localOrder.OrderId, "ACCEPTED");

                await ArrangeAndPayForLogisticsAsync(localOrder, supplierOrderResponse, sourcedInfo.Name);
                await UpdateOrderStatusAsync(localOrder.OrderId, "IN_TRANSIT");

                _logger.LogInformation("Successfully completed procurement workflow for local order {OrderId}.", localOrder.OrderId);
            }
            catch (ProcurementStepFailedException ex)
            {
                _logger.LogError(ex, "Procurement workflow failed for {Material} from {Supplier}.", sourcedInfo.MaterialDetails.MaterialName, sourcedInfo.Name);
                if (localOrder != null)
                {
                    await UpdateOrderStatusAsync(localOrder.OrderId, ex.FailureStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An unexpected critical error occurred during procurement workflow.");
                if (localOrder != null)
                {
                    // A critical, unexpected error is a disaster.
                    await UpdateOrderStatusAsync(localOrder.OrderId, "DISASTER");
                }
            }
        }

        private async Task<SupplierOrderResponse> PlaceSupplierOrderAsync(SourcedSupplier sourcedInfo, int quantityToBuy)
        {
            var orderRequest = new SupplierOrderRequest { MaterialName = sourcedInfo.MaterialDetails.MaterialName, WeightQuantity = quantityToBuy };
            var orderResponse = await sourcedInfo.Client.PlaceOrderAsync(orderRequest);
            if (orderResponse == null)
            {
                throw new ProcurementStepFailedException("Order placement with supplier failed.", "REJECTED");
            }
            return orderResponse;
        }

        private async Task PaySupplierAsync(MaterialOrder localOrder, SupplierOrderResponse supplierOrder, string supplierName)
        {
            if (string.IsNullOrEmpty(supplierOrder.BankAccount))
            {
                throw new ProcurementStepFailedException("Supplier response missing bank account.", "REJECTED");
            }

            try
            {
                await _bankClient.MakePaymentAsync(supplierOrder.BankAccount, supplierName, supplierOrder.Price, $"Order {supplierOrder.OrderId}");
            }
            catch (ApiCallFailedException ex)
            {
                throw new ProcurementStepFailedException($"Payment to supplier failed: {ex.Message}", "PAYMENT_FAILED", ex);
            }

        }

        private async Task ArrangeAndPayForLogisticsAsync(MaterialOrder localOrder, SupplierOrderResponse supplierOrder, string supplierName)
        {
            var pickupReq = new LogisticsPickupRequest
            {
                OriginalExternalOrderId = supplierOrder.OrderId.ToString(),
                OriginCompanyId = supplierName,
                DestinationCompanyId = OurCompanyId,
                Items = [new LogisticsItem { Name = localOrder.Material!.MaterialName, Quantity = localOrder.RemainingAmount }]
            };

            var pickupResp = await _logisticsClient.ArrangePickupAsync(pickupReq);
            if (pickupResp == null || string.IsNullOrEmpty(pickupResp.BulkLogisticsBankAccountNumber))
            {
                throw new ProcurementStepFailedException("Arranging pickup with Bulk Logistics failed.", "LOGISTICS_FAILED");
            }

            var pickupRequest = await CreatePickupRequestAsync(order.OrderId, quantity, materialName);

            if (pickupRequest == null)
                return false;

            var paymentSuccess = await _bankClient.MakePaymentAsync(pickupResp.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResp.Cost, $"Pickup for order {order.OrderId}");

            return paymentSuccess == string.Empty;
            try
            {
                await _bankClient.MakePaymentAsync(pickupResp.BulkLogisticsBankAccountNumber, LogisticsProviderName, pickupResp.Cost, $"Pickup for order {supplierOrder.OrderId}");
            }
            catch (ApiCallFailedException ex)
            {
                throw new ProcurementStepFailedException($"Payment to logistics failed: {ex.Message}", "LOGISTICS_FAILED", ex);
            }
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