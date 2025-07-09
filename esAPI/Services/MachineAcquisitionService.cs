using System.Text.Json;
using esAPI.Clients;

namespace esAPI.Services
{
    public interface IMachineAcquisitionService
    {
        Task<bool> CheckTHOHForMachines();
        Task<(int? orderId, int quantity)> PurchaseMachineViaBank();
        Task QueryOrderDetailsFromTHOH();
        Task PlaceBulkLogisticsPickup(int thohOrderId, int quantity);
    }

    public class MachineAcquisitionService(IHttpClientFactory httpClientFactory, BankService bankService, ICommercialBankClient bankClient) : IMachineAcquisitionService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly BankService _bankService = bankService;
        private readonly ICommercialBankClient _bankClient = bankClient;

        // Returns true if 'electronics_machine' is available
        public async Task<bool> CheckTHOHForMachines()
        {
            var client = _httpClientFactory.CreateClient("thoh");
            var response = await client.GetAsync("/machines");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var machines = doc.RootElement.GetProperty("machines");
            foreach (var machine in machines.EnumerateArray())
            {
                if (machine.GetProperty("machineName").GetString() == "electronics_machine" && machine.GetProperty("quantity").GetInt32() > 0)
                {
                    return true;
                }
            }
            return false;
        }

        // Orders as many 'electronics_machine' as 20% of current bank balance allows, pays via Commercial Bank
        public async Task<(int? orderId, int quantity)> PurchaseMachineViaBank()
        {
            // 1. Get current bank balance
            var balance = await _bankService.GetAndStoreBalance(0); // dayNumber not needed for logic
            var budget = balance * 0.2m;

            // 2. Get machine price and available quantity from THOH
            var thohClient = _httpClientFactory.CreateClient("thoh");
            var machinesResp = await thohClient.GetAsync("/machines");
            machinesResp.EnsureSuccessStatusCode();
            var machinesContent = await machinesResp.Content.ReadAsStringAsync();
            using var machinesDoc = JsonDocument.Parse(machinesContent);
            var machines = machinesDoc.RootElement.GetProperty("machines");
            int available = 0;
            decimal price = 0;
            foreach (var machine in machines.EnumerateArray())
            {
                if (machine.GetProperty("machineName").GetString() == "electronics_machine")
                {
                    available = machine.GetProperty("quantity").GetInt32();
                    price = machine.GetProperty("price").GetDecimal();
                    break;
                }
            }
            if (available == 0 || price == 0) return (null, 0);

            // 3. Calculate how many to buy
            int toBuy = (int)Math.Floor(budget / price);
            if (toBuy == 0) return (null, 0);
            toBuy = Math.Min(toBuy, available);

            // 4. Place order with THOH
            var orderReq = new
            {
                machineName = "electronics_machine",
                quantity = toBuy
            };
            // TODO: Add to our own Machine orders table
            var orderResp = await thohClient.PostAsJsonAsync("/machines", orderReq);
            orderResp.EnsureSuccessStatusCode();
            var orderContent = await orderResp.Content.ReadAsStringAsync();
            using var orderDoc = JsonDocument.Parse(orderContent);
            var order = orderDoc.RootElement;
            var totalPrice = order.GetProperty("totalPrice").GetDecimal();
            var thohBankAccount = order.GetProperty("bankAccount").GetString();
            var orderId = order.TryGetProperty("orderId", out var idProp) ? idProp.GetInt32() : (int?)null;

            if (string.IsNullOrEmpty(thohBankAccount))
                throw new InvalidOperationException("THOH bank account is missing from order response.");

            // 5. Pay THOH via Commercial Bank
            var txnNumber = await _bankClient.MakePaymentAsync(
                thohBankAccount,
                "thoh",
                totalPrice,
                $"Purchase {toBuy} electronics_machine from THOH"
            );
            return (orderId, toBuy);
        }

        public async Task PlaceBulkLogisticsPickup(int thohOrderId, int quantity)
        {
            // 1. Place pickup request with Bulk Logistics
            var bulkClient = _httpClientFactory.CreateClient("bulk-logistics");
            var pickupReq = new
            {
                originalExternalOrderId = thohOrderId.ToString(),
                originCompanyId = "thoh",
                destinationCompanyId = "1",
                items = new[]
                {
                    new { name = "electronics_machine", quantity = quantity }
                }
            };
            var pickupResp = await bulkClient.PostAsJsonAsync("/api/pickup-request", pickupReq);
            pickupResp.EnsureSuccessStatusCode();
            var pickupContent = await pickupResp.Content.ReadAsStringAsync();
            using var pickupDoc = JsonDocument.Parse(pickupContent);
            var pickup = pickupDoc.RootElement;
            var cost = pickup.GetProperty("cost").GetDecimal();
            var bulkBankAccount = pickup.GetProperty("bulkLogisticsBankAccountNumber").GetString();

            // 2. Pay Bulk Logistics
            await _bankClient.MakePaymentAsync(
                bulkBankAccount!,
                "commercial-bank",
                cost,
                $"Pickup {quantity} electronics_machine from THOH order {thohOrderId}"
            );
        }

        public Task QueryOrderDetailsFromTHOH() => Task.CompletedTask;
    }
}