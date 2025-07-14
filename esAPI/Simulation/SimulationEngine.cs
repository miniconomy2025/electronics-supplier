using esAPI.Data;
using esAPI.Simulation.Tasks;
using esAPI.Services;
using esAPI.Clients;
using Microsoft.Extensions.Logging;
using esAPI.DTOs;

namespace esAPI.Simulation
{
    public class SimulationEngine(AppDbContext context, BankService bankService, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, IStartupCostCalculator costCalculator, ICommercialBankClient bankClient, RecyclerApiClient recyclerClient, ILogger<SimulationEngine> logger, RetryQueuePublisher retryQueuePublisher)
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;
        private readonly IStartupCostCalculator _costCalculator = costCalculator;
        private readonly BankService _bankService = bankService;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly RecyclerApiClient _recyclerClient = recyclerClient;
        private readonly ILogger<SimulationEngine> _logger = logger;
        private readonly RetryQueuePublisher _retryQueuePublisher = retryQueuePublisher;

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
            _logger.LogInformation("\n =============== 🏃‍♂️ Starting simulation day {DayNumber} ===============\n", dayNumber);
            

            if (dayNumber == 1)
            {
                _logger.LogInformation("🎬 Executing startup sequence for day 1");
                await ExecuteStartupSequence();
            }
        // ✅ Step 0: Ensure bank account is created before running sim logic
            var company = _context.Companies.FirstOrDefault(c => c.CompanyId == 1);
            if (company == null)
            {
                _logger.LogError("❌ Company not found in database. Simulation halted.");
                return;
            }

            if (string.IsNullOrWhiteSpace(company.BankAccountNumber))
            {
                _logger.LogWarning("⏸️ Bank account not yet created. Skipping simulation day {DayNumber}. Retry will be handled separately.", dayNumber);
                return;
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
            List<SupplierMaterialInfo> recyclerMaterials = new List<SupplierMaterialInfo>();

            try
            {
                recyclerMaterials = await _recyclerClient.GetAvailableMaterialsAsync();

                if (recyclerMaterials == null || recyclerMaterials.Count == 0)
                {
                    _logger.LogWarning("⚠️ Recycler materials fetch returned empty. Enqueueing retry job.");

                    var retryJob = new RecyclerMaterialsFetchRetryJob
                    {
                        RetryAttempt = 1
                    };
                    await _retryQueuePublisher.PublishAsync(retryJob);
                }
                else
                {
                    foreach (var mat in recyclerMaterials)
                    {
                        _logger.LogInformation($"[Recycler] {mat.MaterialName}: AvailableQuantity={mat.AvailableQuantity}, PricePerKg={mat.PricePerKg}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to fetch recycler materials. Enqueueing retry job.");

                var retryJob = new RecyclerMaterialsFetchRetryJob
                {
                    RetryAttempt = 1
                };
                await _retryQueuePublisher.PublishAsync(retryJob);
            }

            foreach (var mat in recyclerMaterials)
            {
                _logger.LogInformation($"[Recycler] {mat.MaterialName}: AvailableQuantity={mat.AvailableQuantity}, PricePerKg={mat.PricePerKg}");
            }

            // 3. Query our own copper and silicon stock
            var ownSupplies = _context.CurrentSupplies.ToList();
            int ownCopper = ownSupplies.FirstOrDefault(s => s.MaterialName.ToLower() == "copper")?.AvailableSupply ?? 0;
            int ownSilicon = ownSupplies.FirstOrDefault(s => s.MaterialName.ToLower() == "silicon")?.AvailableSupply ?? 0;
            _logger.LogInformation($"[Stock] Our Copper: {ownCopper}, Our Silicon: {ownSilicon}");

            // 4. Place orders and pay if our stock is low
            foreach (var mat in recyclerMaterials)
            {
                int ownStock = mat.MaterialName.ToLower() == "copper" ? ownCopper :
                                mat.MaterialName.ToLower() == "silicon" ? ownSilicon : 0;
                if (ownStock < 1000 && mat.AvailableQuantity > 0)
                {
                    int orderQty = mat.AvailableQuantity / 2;
                    if (orderQty == 0) continue;
                    _logger.LogInformation($"[Order] Placing recycler order for {orderQty} kg of {mat.MaterialName} (our stock: {ownStock})");
                    var orderResponse = await _recyclerClient.PlaceRecyclerOrderAsync(mat.MaterialName, orderQty);
                    if (orderResponse?.IsSuccess == true && orderResponse.Data != null)
                    {
                        var orderId = orderResponse.Data.OrderId;
                        var total = orderResponse.Data.Total;
                        var accountNumber = orderResponse.Data.AccountNumber;
                        _logger.LogInformation($"[Order] Recycler order placed: OrderId={orderId}, Total={total}, Account={accountNumber}");
                        if (!string.IsNullOrEmpty(accountNumber) && total > 0)
                        {
                            _logger.LogInformation($"[Payment] Paying recycler {total} for order {orderId}");
                            try
                            {
                                await _bankClient.MakePaymentAsync(accountNumber, "commercial-bank", total, orderId.ToString());
                                _logger.LogInformation($"[Payment] Payment sent for recycler order {orderId}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"❌ Payment failed for order {orderId}. Enqueueing payment retry.");

                                var paymentRetryJob = new PaymentRetryJob
                                {
                                    RetryAttempt = 1,
                                    ToAccountNumber = accountNumber,
                                    ToBankName = "commercial-bank",
                                    Amount = total,
                                    Description = orderId.ToString()
                                };

                                await _retryQueuePublisher.PublishAsync(paymentRetryJob);
                            }
                            _logger.LogInformation($"[Payment] Payment sent for recycler order {orderId}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"[Order] Failed to place recycler order for {mat.MaterialName}, enqueueing retry job.");

                        // Enqueue retry job for failed order
                        var retryJob = new RecyclerOrderRetryJob
                        {
                            JobType = "RecyclerOrderRetry", // set this string accordingly
                            RetryAttempt = 1,
                            MaterialName = mat.MaterialName,
                            QuantityInKg = orderQty
                        };
                        await _retryQueuePublisher.PublishAsync(retryJob);
                                    
                    }
                }
                else
                {
                    _logger.LogInformation($"[Order] No order placed for {mat.MaterialName} (our stock: {ownStock}, recycler available: {mat.AvailableQuantity})");
                }
            }

            if (OnDayAdvanced != null)
            {
                _logger.LogInformation("📡 Triggering OnDayAdvanced event for day {DayNumber}", dayNumber);
                await OnDayAdvanced(dayNumber);
            }
            
            _logger.LogInformation("✅ Simulation day {DayNumber} completed successfully", dayNumber);
        }

        private async Task<bool> ExecuteStartupSequence()
        {
            _logger.LogInformation("🏦 Setting up bank account");
            var bankSetupResult = await _bankAccountService.SetupBankAccountAsync();

            const decimal loanAmount = 20000000m; // 20 million

            if (!bankSetupResult.Success)
            {
                _logger.LogWarning("⏳ Bank account setup failed. Retry job has been scheduled. Will retry later.");

                // 🔁 Still queue the loan retry job
                var retryJob = new LoanRequestRetryJob
                {
                    RetryAttempt = 1,
                    Amount = loanAmount
                };
                await _retryQueuePublisher.PublishAsync(retryJob);

                return true;
            }

            _logger.LogInformation("✅ Bank account setup completed");

            _logger.LogInformation("🏦 Requesting loan for startup costs");
            string? loanSuccess = await _bankClient.RequestLoanAsync(loanAmount);

            if (loanSuccess == null)
            {
                _logger.LogWarning("❌ Initial loan request failed. Publishing retry job.");

                var retryJob = new LoanRequestRetryJob
                {
                    RetryAttempt = 1,
                    Amount = loanAmount
                };
                await _retryQueuePublisher.PublishAsync(retryJob);

                return true;
            }

            _logger.LogInformation("✅ Loan requested successfully: {LoanNumber}", loanSuccess);
            _logger.LogInformation("✅ Startup sequence completed successfully");

            return true;
        }

    }
} 