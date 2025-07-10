using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using esAPI.Services;
using esAPI.Clients;
using Microsoft.EntityFrameworkCore;
using esAPI.Data;

namespace esAPI.Services
{
    public interface IMaterialAcquisitionService
    {
        Task PurchaseMaterialsViaBank();
        Task<int> PlaceBulkLogisticsPickup(int orderId, string itemName, int quantity, string supplier);
    }

    public class MaterialAcquisitionService : IMaterialAcquisitionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BankService _bankService;
        private readonly ICommercialBankClient _bankClient;

        private readonly AppDbContext _context;

        public MaterialAcquisitionService(IHttpClientFactory httpClientFactory, BankService bankService, ICommercialBankClient bankClient, AppDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _bankService = bankService;
            _bankClient = bankClient;
            _context = context;
        }

        public async Task PurchaseMaterialsViaBank()
        {
            // 1. Copper: buy half the available stock from the cheapest supplier
            await PurchaseMaterial("copper", buyHalf: true);
            // 2. Silicon: buy as much as possible with 30% of current bank balance
            await PurchaseMaterial("silicon", buyHalf: false);
        }

        private async Task PurchaseMaterial(string materialName, bool buyHalf)
        {
            // Query both suppliers
            var thohClient = _httpClientFactory.CreateClient("thoh");
            var recyclerClient = _httpClientFactory.CreateClient("recycler");
            var thohResp = await thohClient.GetAsync("/raw-materials");
            thohResp.EnsureSuccessStatusCode();
            var thohContent = await thohResp.Content.ReadAsStringAsync();
            var thohMaterials = JsonDocument.Parse(thohContent).RootElement;
            var recyclerResp = await recyclerClient.GetAsync("/raw-materials");
            recyclerResp.EnsureSuccessStatusCode();
            var recyclerContent = await recyclerResp.Content.ReadAsStringAsync();
            var recyclerMaterials = JsonDocument.Parse(recyclerContent).RootElement;

            // Find material in each supplier
            var thohMat = thohMaterials.EnumerateArray().FirstOrDefault(m => m.GetProperty("rawMaterialName").GetString()?.ToLower() == materialName);
            var recyclerMat = recyclerMaterials.EnumerateArray().FirstOrDefault(m => m.GetProperty("rawMaterialName").GetString()?.ToLower() == materialName);

            // Find cheapest supplier with stock
            decimal thohPrice = thohMat.ValueKind != JsonValueKind.Undefined ? thohMat.GetProperty("pricePerKg").GetDecimal() : decimal.MaxValue;
            int thohQty = thohMat.ValueKind != JsonValueKind.Undefined ? thohMat.GetProperty("quantityAvailable").GetInt32() : 0;
            decimal recyclerPrice = recyclerMat.ValueKind != JsonValueKind.Undefined ? recyclerMat.GetProperty("pricePerKg").GetDecimal() : decimal.MaxValue;
            int recyclerQty = recyclerMat.ValueKind != JsonValueKind.Undefined ? recyclerMat.GetProperty("quantityAvailable").GetInt32() : 0;

            string supplier = null;
            HttpClient supplierClient = null;
            decimal pricePerKg = 0;
            int availableQty = 0;
            if (thohQty > 0 && thohPrice <= recyclerPrice)
            {
                supplier = "thoh";
                supplierClient = thohClient;
                pricePerKg = thohPrice;
                availableQty = thohQty;
            }
            else if (recyclerQty > 0)
            {
                supplier = "recycler";
                supplierClient = recyclerClient;
                pricePerKg = recyclerPrice;
                availableQty = recyclerQty;
            }
            if (supplier == null || availableQty == 0 || supplierClient == null) return;

            // Determine quantity to buy
            int quantityToBuy = 0;
            if (buyHalf)
            {
                quantityToBuy = availableQty / 2;
            }
            else
            {
                var balance = await _bankService.GetAndStoreBalance(0); // dayNumber not needed
                var budget = balance * 0.3m;
                quantityToBuy = (int)Math.Floor(budget / pricePerKg);
                quantityToBuy = Math.Min(quantityToBuy, availableQty);
            }
            if (quantityToBuy == 0) return;

            // Place order
            var orderReq = new
            {
                materialName = materialName,
                weightQuantity = quantityToBuy
            };
            var orderResp = await supplierClient.PostAsJsonAsync("/raw-materials", orderReq);
            orderResp.EnsureSuccessStatusCode();
            var orderContent = await orderResp.Content.ReadAsStringAsync();
            using var orderDoc = JsonDocument.Parse(orderContent);
            var order = orderDoc.RootElement;
            var totalPrice = order.GetProperty("price").GetDecimal();
            var supplierBankAccount = order.GetProperty("bankAccount").GetString();
            var orderId = order.TryGetProperty("orderId", out var idProp) ? idProp.GetInt32() : 0;

            if (string.IsNullOrEmpty(supplierBankAccount))
                throw new InvalidOperationException($"{supplier} bank account is missing from order response.");

            // Pay supplier
            await _bankClient.MakePaymentAsync(
                supplierBankAccount!,
                supplier,
                totalPrice,
                $"Purchase {quantityToBuy}kg {materialName} from {supplier}"
            );

            int pickupRequestId = await PlaceBulkLogisticsPickup(orderId, materialName, quantityToBuy, supplier);

            // Save pickupRequestId to material_orders table
            var orderEntity = await _context.MaterialOrders
                .FirstOrDefaultAsync(o => o.ExternalOrderId == orderId && o.Supplier!.CompanyName.ToLower() == supplier);

            if (orderEntity != null)
            {
                orderEntity.PickupRequestId = pickupRequestId;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> PlaceBulkLogisticsPickup(int orderId, string materialName, int quantity, string supplier)
        {
            var bulkClient = _httpClientFactory.CreateClient("bulk-logistics");
            var pickupReq = new
            {
                originalExternalOrderId = orderId.ToString(),
                originCompanyId = supplier,
                destinationCompanyId = "1",
                items = new[]
                {
                    new { name = materialName, quantity = quantity }
                }
            };
            var pickupResp = await bulkClient.PostAsJsonAsync("/api/pickup-request", pickupReq);
            pickupResp.EnsureSuccessStatusCode();
            var pickupContent = await pickupResp.Content.ReadAsStringAsync();
            using var pickupDoc = JsonDocument.Parse(pickupContent);
            var pickup = pickupDoc.RootElement;
            var pickupRequestId = pickup.GetProperty("pickupRequestId").GetInt32(); // as INT now
            var cost = pickup.GetProperty("cost").GetDecimal();
            var bulkBankAccount = pickup.GetProperty("bulkLogisticsBankAccountNumber").GetString();

            // Pay Bulk Logistics
            await _bankClient.MakePaymentAsync(
                bulkBankAccount!,
                "commercial-bank",
                cost,
                $"Pickup {quantity}kg {materialName} from {supplier} order {orderId}"
            );

            return pickupRequestId;
        }
    }
} 