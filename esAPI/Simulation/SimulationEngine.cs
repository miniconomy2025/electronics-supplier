using esAPI.Data;
using esAPI.Simulation.Tasks;
using esAPI.Services;
using esAPI.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using esAPI.DTOs;
using esAPI.Interfaces;
using esAPI.Interfaces.Services;
using System.Net.Http;

namespace esAPI.Simulation
{
    public class SimulationEngine(AppDbContext context, BankService bankService, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, IStartupCostCalculator costCalculator, ICommercialBankClient bankClient, RecyclerApiClient recyclerClient, IBulkLogisticsClient bulkLogisticsClient, ISimulationStateService simulationStateService, IElectronicsService electronicsService, IHttpClientFactory httpClientFactory, ThohApiClient thohApiClient, ILogger<SimulationEngine> logger)
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;
        private readonly IStartupCostCalculator _costCalculator = costCalculator;
        private readonly BankService _bankService = bankService;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly RecyclerApiClient _recyclerClient = recyclerClient;
        private readonly IBulkLogisticsClient _bulkLogisticsClient = bulkLogisticsClient;
        private readonly ISimulationStateService _simulationStateService = simulationStateService;
        private readonly IElectronicsService _electronicsService = electronicsService;
        private readonly ILogger<SimulationEngine> _logger = logger;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ThohApiClient _thohApiClient = thohApiClient;

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
            _logger.LogInformation("\n =============== 🏃‍♂️ Starting simulation day {DayNumber} ===============\n", dayNumber);
            
            if (dayNumber == 1)
            {
                _logger.LogInformation("🎬 Executing startup sequence for day 1");
                await ExecuteStartupSequence();
            }
            else
            {
                // Loan logic at the start of the day (not on day 1)
                try
                {
                    var balance = await _bankService.GetAndStoreBalance(dayNumber);
                    _logger.LogInformation("💰 Current bank balance at start of day {DayNumber}: {Balance}", dayNumber, balance);
                    if (balance <= 10000m)
                    {
                        _logger.LogInformation("🏦 Bank balance is low (<= 10,000). Attempting to request a loan...");
                        const decimal loanAmount = 20000000m; // 20 million
                        string? loanSuccess = await _bankClient.RequestLoanAsync(loanAmount);
                        if (loanSuccess == null)
                        {
                            _logger.LogWarning("❌ Failed to request loan for day {DayNumber}. Will retry next day if still low.", dayNumber);
                        }
                        else
                        {
                            _logger.LogInformation("✅ Loan requested successfully for day {DayNumber}: {LoanNumber}", dayNumber, loanSuccess);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error while checking balance or requesting loan at start of day {DayNumber}", dayNumber);
                }
            }
            
            _logger.LogInformation("📊 Running simulation logic for Day {DayNumber}", dayNumber);

            // 1. Query bank and store our balance
            _logger.LogInformation("🏦 Querying bank balance for day {DayNumber}", dayNumber);
            try
            {
                var balance = await _bankService.GetAndStoreBalance(dayNumber);
                _logger.LogInformation("✅ Bank balance stored for day {DayNumber}: {Balance}", dayNumber, balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bank balance retrieval failed for day {DayNumber}, but simulation will continue", dayNumber);
            }

            // 2. Query recycler for copper and silicon stock
            var recyclerMaterials = await _recyclerClient.GetAvailableMaterialsAsync();
            foreach (var mat in recyclerMaterials)
            {
                _logger.LogInformation($"[Recycler] {mat.MaterialName}: AvailableQuantity={mat.AvailableQuantity}, PricePerKg={mat.PricePerKg}");
            }

            // 3. Query our own copper and silicon stock
            var ownSupplies = _context.CurrentSupplies.ToList();
            int ownCopper = ownSupplies.FirstOrDefault(s => s.MaterialName.ToLower() == "copper")?.AvailableSupply ?? 0;
            int ownSilicon = ownSupplies.FirstOrDefault(s => s.MaterialName.ToLower() == "silicon")?.AvailableSupply ?? 0;
            _logger.LogInformation($"[Stock] Our Copper: {ownCopper}, Our Silicon: {ownSilicon}");

            // --- Machine purchase logic ---
            try
            {
                int totalMachines = await _context.Machines.CountAsync(m => m.RemovedAt == null);
                int brokenMachines = await _context.Machines.CountAsync(m => m.MachineStatusId == (int)Models.Enums.Machine.Status.Broken && m.RemovedAt == null);
                // FIX: Order machines if we have zero or all are broken
                if (totalMachines == 0 || totalMachines == brokenMachines)
                {
                    _logger.LogInformation($"[Machine] No working machines. Attempting to buy 2 new machines from THOH.");
                    try
                    {
                        var thohHttpClient = _httpClientFactory.CreateClient("thoh");
                        var machineOrderReq = new { machineName = "electronics_machine", quantity = 2 };
                        var response = await thohHttpClient.PostAsJsonAsync("/machines", machineOrderReq);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            using var doc = System.Text.Json.JsonDocument.Parse(content);
                            var orderId = doc.RootElement.GetProperty("orderId").GetInt32();
                            var totalPrice = doc.RootElement.GetProperty("totalPrice").GetDecimal();
                            var bankAccount = doc.RootElement.GetProperty("bankAccount").GetString();
                            _logger.LogInformation($"[Machine] Ordered 2 new machines from THOH. OrderId={orderId}, TotalPrice={totalPrice}, BankAccount={bankAccount}");
                            if (!string.IsNullOrEmpty(bankAccount) && totalPrice > 0)
                            {
                                try
                                {
                                    await _bankClient.MakePaymentAsync(bankAccount, "thoh", totalPrice, $"Purchase 2 electronics_machine from THOH");
                                    _logger.LogInformation($"[Machine] Payment sent to THOH for order {orderId}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"[Machine] Error paying THOH for machine order {orderId}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"[Machine] Failed to order machines from THOH. Status: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Machine] Exception during machine purchase from THOH");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine] Error checking machine status or purchasing new machines");
            }

            // 4. Place orders and pay if our stock is low
            var materialsToOrder = new[] { "copper", "silicon" };
            foreach (var materialName in materialsToOrder)
            {
                int ownStock = materialName == "copper" ? ownCopper : ownSilicon;
                if (ownStock < 1000)
                {
                    bool orderedFromThoh = false;
                    try
                    {
                        int thohQty = 0;
                        var thohMaterials = await _thohApiClient.GetAvailableMaterialsAsync();
                        var thohMat = thohMaterials?.FirstOrDefault(m => m.MaterialName.ToLower() == materialName);
                        if (thohMat != null && thohMat.AvailableQuantity > 0)
                        {
                            thohQty = thohMat.AvailableQuantity / 2;
                            if (thohQty > 0)
                            {
                                _logger.LogInformation($"[Order] Attempting THOH order for {thohQty} kg of {materialName} (our stock: {ownStock})");
                                var thohOrderReq = new SupplierOrderRequest { MaterialName = materialName, WeightQuantity = thohQty };
                                var thohOrderResp = await _thohApiClient.PlaceOrderAsync(thohOrderReq);
                                if (thohOrderResp != null && !string.IsNullOrEmpty(thohOrderResp.BankAccount))
                                {
                                    orderedFromThoh = true;
                                    _logger.LogInformation($"[Order] THOH order placed: OrderId={thohOrderResp.OrderId}, Total={thohOrderResp.Price}, Account={thohOrderResp.BankAccount}");
                                    // Insert into material_orders (THOH as supplier)
                                    try
                                    {
                                        var thohCompany = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName.ToLower() == "thoh");
                                        var material = await _context.Materials.FirstOrDefaultAsync(m => m.MaterialName.ToLower() == materialName);
                                        if (thohCompany != null && material != null)
                                        {
                                            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
                                            var orderedAt = sim != null ? sim.DayNumber : dayNumber;
                                            var newOrder = new Models.MaterialOrder
                                            {
                                                SupplierId = thohCompany.CompanyId,
                                                MaterialId = material.MaterialId,
                                                ExternalOrderId = thohOrderResp.OrderId,
                                                RemainingAmount = thohQty,
                                                TotalAmount = thohQty,
                                                OrderStatusId = 1, // Pending
                                                OrderedAt = orderedAt,
                                            };
                                            _context.MaterialOrders.Add(newOrder);
                                            await _context.SaveChangesAsync();
                                            _logger.LogInformation($"[DB] Inserted material order for THOH: Material={materialName}, Qty={thohQty}, OrderId={thohOrderResp.OrderId}");
                                            // --- Bulk Logistics Integration ---
                                            try
                                            {
                                                var pickupRequest = new LogisticsPickupRequest
                                                {
                                                    OriginalExternalOrderId = thohOrderResp.OrderId.ToString(),
                                                    OriginCompanyId = "thoh",
                                                    DestinationCompanyId = "electronics-supplier",
                                                    Items = new[] { new LogisticsItem { Name = materialName, Quantity = thohQty } }
                                                };
                                                var pickupResponse = await _bulkLogisticsClient.ArrangePickupAsync(pickupRequest);
                                                if (pickupResponse != null && !string.IsNullOrEmpty(pickupResponse.BulkLogisticsBankAccountNumber))
                                                {
                                                    _logger.LogInformation($"[Logistics] Pickup arranged. Paying {pickupResponse.Cost} to Bulk Logistics.");
                                                    try
                                                    {
                                                        await _bankClient.MakePaymentAsync(pickupResponse.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResponse.Cost, $"Pickup for THOH order {thohOrderResp.OrderId}");
                                                        _logger.LogInformation($"[Logistics] Payment sent to Bulk Logistics for pickup of order {thohOrderResp.OrderId}");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        _logger.LogError(ex, $"[Logistics] Error paying Bulk Logistics for pickup of order {thohOrderResp.OrderId}");
                                                    }
                                                    // Insert into pickup_requests table
                                                    try
                                                    {
                                                        var pickupDb = new Models.PickupRequest
                                                        {
                                                            ExternalRequestId = thohOrderResp.OrderId,
                                                            Type = materialName == "copper" ? Models.Enums.PickupRequest.PickupType.COPPER : Models.Enums.PickupRequest.PickupType.SILICONE,
                                                            Quantity = thohQty,
                                                            PlacedAt = (double)_simulationStateService.GetCurrentSimulationTime(3)
                                                        };
                                                        _context.PickupRequests.Add(pickupDb);
                                                        await _context.SaveChangesAsync();
                                                        _logger.LogInformation($"[DB] Inserted pickup request for Bulk Logistics: OrderId={thohOrderResp.OrderId}, Material={materialName}, Qty={thohQty}");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        _logger.LogError(ex, $"[DB] Error inserting pickup request for Bulk Logistics: OrderId={thohOrderResp.OrderId}, Material={materialName}");
                                                    }
                                                }
                                                else
                                                {
                                                    _logger.LogWarning($"[Logistics] Failed to arrange pickup with Bulk Logistics for order {thohOrderResp.OrderId}");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, $"[Logistics] Exception during Bulk Logistics integration for order {thohOrderResp.OrderId}");
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning($"[DB] Could not insert material order for THOH: missing company or material. Material={materialName}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, $"[DB] Exception inserting material order for THOH: Material={materialName}");
                                    }
                                    if (!string.IsNullOrEmpty(thohOrderResp.BankAccount) && thohOrderResp.Price > 0)
                                    {
                                        _logger.LogInformation($"[Payment] Paying THOH {thohOrderResp.Price} for order {thohOrderResp.OrderId}");
                                        await _bankClient.MakePaymentAsync(thohOrderResp.BankAccount, "thoh", thohOrderResp.Price, thohOrderResp.OrderId.ToString());
                                        _logger.LogInformation($"[Payment] Payment sent for THOH order {thohOrderResp.OrderId}");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"[Order] Failed to place THOH order for {materialName}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"[Order] THOH has no available {materialName} or zero quantity. Will attempt Recycler fallback.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[Order] Exception during THOH order for {materialName}. Will attempt Recycler fallback.");
                    }
                    if (orderedFromThoh)
                    {
                        _logger.LogInformation($"[Order] Successfully ordered {materialName} from THOH. Skipping Recycler.");
                        continue; // Skip Recycler if THOH order succeeded
                    }
                    // --- Recycler fallback ---
                    _logger.LogInformation($"[Order] Attempting Recycler fallback for {materialName}.");
                    var mat = recyclerMaterials.FirstOrDefault(m => m.MaterialName.ToLower() == materialName);
                    if (mat != null && mat.AvailableQuantity > 0)
                    {
                        int orderQty = mat.AvailableQuantity / 2;
                        if (orderQty == 0)
                        {
                            _logger.LogInformation($"[Order] Recycler available quantity for {materialName} is zero after division. Skipping order.");
                            continue;
                        }
                        _logger.LogInformation($"[Order] Placing recycler order for {orderQty} kg of {mat.MaterialName} (our stock: {ownStock})");
                        var orderResponse = await _recyclerClient.PlaceRecyclerOrderAsync(mat.MaterialName, orderQty);
                        if (orderResponse?.IsSuccess == true && orderResponse.Data != null)
                        {
                            var orderId = orderResponse.Data.OrderId;
                            var total = orderResponse.Data.Total;
                            var accountNumber = orderResponse.Data.AccountNumber;
                            _logger.LogInformation($"[Order] Recycler order placed: OrderId={orderId}, Total={total}, Account={accountNumber}");
                            // Insert into material_orders
                            try
                            {
                                var recyclerCompany = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyName.ToLower() == "recycler");
                                var material = await _context.Materials.FirstOrDefaultAsync(m => m.MaterialName.ToLower() == mat.MaterialName.ToLower());
                                if (recyclerCompany != null && material != null)
                                {
                                    var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
                                    var orderedAt = sim != null ? sim.DayNumber : dayNumber;
                                    var newOrder = new Models.MaterialOrder
                                    {
                                        SupplierId = recyclerCompany.CompanyId,
                                        MaterialId = material.MaterialId,
                                        ExternalOrderId = orderId,
                                        RemainingAmount = orderQty,
                                        TotalAmount = orderQty,
                                        OrderStatusId = 1, // Pending
                                        OrderedAt = orderedAt,
                                    };
                                    _context.MaterialOrders.Add(newOrder);
                                    await _context.SaveChangesAsync();
                                    _logger.LogInformation($"[DB] Inserted material order for recycler: Material={mat.MaterialName}, Qty={orderQty}, OrderId={orderId}");
                                    // --- Bulk Logistics Integration ---
                                    try
                                    {
                                        var pickupRequest = new LogisticsPickupRequest
                                        {
                                            OriginalExternalOrderId = orderId.ToString(),
                                            OriginCompanyId = "recycler",
                                            DestinationCompanyId = "electronics-supplier",
                                            Items = new[] { new LogisticsItem { Name = mat.MaterialName.ToLower(), Quantity = orderQty } }
                                        };
                                        var pickupResponse = await _bulkLogisticsClient.ArrangePickupAsync(pickupRequest);
                                        if (pickupResponse != null && !string.IsNullOrEmpty(pickupResponse.BulkLogisticsBankAccountNumber))
                                        {
                                            _logger.LogInformation($"[Logistics] Pickup arranged. Paying {pickupResponse.Cost} to Bulk Logistics.");
                                            try
                                            {
                                                await _bankClient.MakePaymentAsync(pickupResponse.BulkLogisticsBankAccountNumber, "commercial-bank", pickupResponse.Cost, $"Pickup for recycler order {orderId}");
                                                _logger.LogInformation($"[Logistics] Payment sent to Bulk Logistics for pickup of order {orderId}");
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, $"[Logistics] Error paying Bulk Logistics for pickup of order {orderId}");
                                            }
                                            // Insert into pickup_requests table
                                            try
                                            {
                                                var pickupDb = new Models.PickupRequest
                                                {
                                                    ExternalRequestId = orderId,
                                                    Type = mat.MaterialName.ToLower() == "copper" ? Models.Enums.PickupRequest.PickupType.COPPER : Models.Enums.PickupRequest.PickupType.SILICONE,
                                                    Quantity = orderQty,
                                                    PlacedAt = (double)_simulationStateService.GetCurrentSimulationTime(3)
                                                };
                                                _context.PickupRequests.Add(pickupDb);
                                                await _context.SaveChangesAsync();
                                                _logger.LogInformation($"[DB] Inserted pickup request for Bulk Logistics: OrderId={orderId}, Material={mat.MaterialName}, Qty={orderQty}");
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, $"[DB] Error inserting pickup request for Bulk Logistics: OrderId={orderId}, Material={mat.MaterialName}");
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning($"[Logistics] Failed to arrange pickup with Bulk Logistics for order {orderId}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, $"[Logistics] Exception during Bulk Logistics integration for order {orderId}");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"[DB] Could not insert material order for recycler: missing company or material. Material={mat.MaterialName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"[DB] Exception inserting material order for recycler: Material={mat.MaterialName}");
                            }
                            if (!string.IsNullOrEmpty(accountNumber) && total > 0)
                            {
                                _logger.LogInformation($"[Payment] Paying recycler {total} for order {orderId}");
                                await _bankClient.MakePaymentAsync(accountNumber, "commercial-bank", total, orderId.ToString());
                                _logger.LogInformation($"[Payment] Payment sent for recycler order {orderId}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"[Order] Failed to place recycler order for {mat.MaterialName}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"[Order] No order placed for {materialName} (our stock: {ownStock}, recycler available: {(mat?.AvailableQuantity ?? 0)})");
                    }
                }
            }

            if (OnDayAdvanced != null)
            {
                _logger.LogInformation("📡 Triggering OnDayAdvanced event for day {DayNumber}", dayNumber);
                await OnDayAdvanced(dayNumber);
            }

            // --- Produce electronics at end of day ---
            try
            {
                var result = await _electronicsService.ProduceElectronicsAsync();
                _logger.LogInformation($"[Production] Produced {result.ElectronicsCreated} electronics. Materials used: {string.Join(", ", result.MaterialsUsed.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Production] Error producing electronics at end of day");
            }
            
            _logger.LogInformation("✅ Simulation day {DayNumber} completed successfully", dayNumber);
        }

        private async Task<bool> ExecuteStartupSequence()
        {
            _logger.LogInformation("🏦 Setting up bank account");
            var bankSetupResult = await _bankAccountService.SetupBankAccountAsync();
            if (!bankSetupResult.Success)
            {
                _logger.LogError("❌ Failed to set up bank account: {Error}", bankSetupResult.Error);
                return false;
            }
            _logger.LogInformation("✅ Bank account setup completed");

            // COMMENTED OUT: Startup cost planning for now
            /*
            // _logger.LogInformation("💰 Generating startup cost plans");
            // var allPlans = await _costCalculator.GenerateAllPossibleStartupPlansAsync();
            // if (!allPlans.Any())
            // {
            //     _logger.LogError("❌ No startup cost plans generated");
            //     return false;
            // }
            // _logger.LogInformation("✅ Generated {PlanCount} startup cost plans", allPlans.Count());

            // var bestPlan = allPlans.OrderBy(p => p.TotalCost).First();
            // _logger.LogInformation("💡 Selected best startup plan with cost: {TotalCost}", bestPlan.TotalCost);
            */

            _logger.LogInformation("🏦 Requesting loan for startup costs");
            const decimal loanAmount = 20000000m; // 20 million
            string? loanSuccess = await _bankClient.RequestLoanAsync(loanAmount);
            if (loanSuccess == null)
            {
                _logger.LogError("❌ Failed to request loan for startup costs");
                return false;
            }
            _logger.LogInformation("✅ Loan requested successfully: {LoanNumber}", loanSuccess);

            _logger.LogInformation("✅ Startup sequence completed successfully");
            return true;
        }
    }
} 