using System.Text.Json;
using esAPI.Clients;
using esAPI.Data;
using Microsoft.EntityFrameworkCore;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;

namespace esAPI.Services
{
    public class MachineAcquisitionService(IHttpClientFactory httpClientFactory, IBankService bankService, ICommercialBankClient bankClient, AppDbContext context, ISimulationStateService stateService) : IMachineAcquisitionService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IBankService _bankService = bankService;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly AppDbContext _context = context;
        private readonly ISimulationStateService _stateService = stateService;

        // Returns true if 'electronics_machine' is available
        public async Task<bool> CheckTHOHForMachines()
        {
            var client = _httpClientFactory.CreateClient("thoh");
            var response = await client.GetAsync("api/machines");
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
            var balance = await _bankService.GetAndStoreBalance(_stateService.CurrentDay);
            var budget = balance * 0.2m;

            // 2. Get machine price and available quantity from THOH
            var thohClient = _httpClientFactory.CreateClient("thoh");
            var machinesResp = await thohClient.GetAsync("api/machines");
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
            var orderResp = await thohClient.PostAsJsonAsync("api/machines", orderReq);
            orderResp.EnsureSuccessStatusCode();
            var orderContent = await orderResp.Content.ReadAsStringAsync();
            using var orderDoc = JsonDocument.Parse(orderContent);
            var order = orderDoc.RootElement;
            var totalPrice = order.GetProperty("totalPrice").GetDecimal();
            var thohBankAccount = order.GetProperty("bankAccount").GetString();
            var orderId = order.TryGetProperty("orderId", out var idProp) ? idProp.GetInt32() : (int?)null;

            // Add to our own Machine orders table
            if (orderId.HasValue)
            {
                var thohCompany = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName.ToLower() == "thoh");
                if (thohCompany != null)
                {
                    var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
                    var currentDay = sim?.DayNumber ?? 1;

                    var machineOrder = new Models.MachineOrder
                    {
                        SupplierId = thohCompany.CompanyId,
                        ExternalOrderId = orderId.Value,
                        RemainingAmount = toBuy,
                        TotalAmount = toBuy,
                        OrderStatusId = 1, // Pending
                        PlacedAt = currentDay
                    };

                    _context.MachineOrders.Add(machineOrder);
                    await _context.SaveChangesAsync();
                }
            }

            if (string.IsNullOrEmpty(thohBankAccount))
                throw new InvalidOperationException("THOH bank account is missing from order response.");

            // 5. Pay THOH via Commercial Bank
            var txnNumber = await _bankClient.MakePaymentAsync(
                thohBankAccount,
                "thoh",
                totalPrice,
                $"Purchase {toBuy} electronics_machine from THOH"
            );


            // 6. Place pickup with Bulk Logistics
            if (orderId.HasValue)
            {
                await PlaceBulkLogisticsPickup(orderId.Value, toBuy);
            }

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
                    new { name = "electronics_machine", quantity }
                }
            };
            var pickupResp = await bulkClient.PostAsJsonAsync("/api/pickup-request", pickupReq);
            pickupResp.EnsureSuccessStatusCode();
            var pickupContent = await pickupResp.Content.ReadAsStringAsync();
            using var pickupDoc = JsonDocument.Parse(pickupContent);
            var pickup = pickupDoc.RootElement;

            var pickupRequestId = pickup.GetProperty("pickupRequestId").GetInt32();
            var cost = pickup.GetProperty("cost").GetDecimal();
            var bulkBankAccount = pickup.GetProperty("accountNumber").GetString();

            // 2. Pay Bulk Logistics
            await _bankClient.MakePaymentAsync(
                bulkBankAccount!,
                "commercial-bank",
                cost,
                pickupRequestId.ToString()
            );

            // 3. Store the pickup request in the pickup_requests table
            var pickupRequest = new Models.PickupRequest
            {
                ExternalRequestId = thohOrderId,
                PickupRequestId = pickupRequestId,
                Type = Models.Enums.PickupRequest.PickupType.MACHINE,
                Quantity = quantity,
                PlacedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            _context.PickupRequests.Add(pickupRequest);
            await _context.SaveChangesAsync();
        }


        public Task QueryOrderDetailsFromTHOH() => Task.CompletedTask;
    }
}
