using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Services;
using esAPI.Simulation;
using esAPI.Interfaces;
using esAPI.Clients;

namespace esAPI.Services
{
    public class SimulationStartupService
    {
        private readonly AppDbContext _context;
        private readonly BankAccountService _bankAccountService;
        private readonly ISimulationStateService _stateService;
        private readonly OrderExpirationBackgroundService _orderExpirationBackgroundService;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<SimulationStartupService> _logger;
        private readonly ThohApiClient _thohApiClient;
        private readonly ElectronicsMachineDetailsService _machineDetailsService;

        public SimulationStartupService(
            AppDbContext context,
            BankAccountService bankAccountService,
            ISimulationStateService stateService,
            OrderExpirationBackgroundService orderExpirationBackgroundService,
            ICommercialBankClient bankClient,
            ILogger<SimulationStartupService> logger,
            ThohApiClient thohApiClient,
            ElectronicsMachineDetailsService machineDetailsService)
        {
            _context = context;
            _bankAccountService = bankAccountService;
            _stateService = stateService;
            _orderExpirationBackgroundService = orderExpirationBackgroundService;
            _bankClient = bankClient;
            _logger = logger;
            _thohApiClient = thohApiClient;
            _machineDetailsService = machineDetailsService;
        }

        public async Task<(bool Success, string? AccountNumber, string? Error)> StartSimulationAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Starting simulation startup process...");
                
                // Start simulation state service
                _stateService.Start();
                _logger.LogInformation("📊 Simulation state service started");

                // Start order expiration background service
                _orderExpirationBackgroundService.StartAsync();
                _logger.LogInformation("⏰ Order expiration background service started");

                // Set up bank account with commercial bank
                _logger.LogInformation("🏦 Setting up bank account with commercial bank...");
                var bankSetupResult = await _bankAccountService.SetupBankAccountAsync();
                if (!bankSetupResult.Success)
                {
                    _logger.LogError("❌ Failed to set up bank account. Error: {Error}", bankSetupResult.Error);
                    return (false, null, $"Failed to set up bank account. Error: {bankSetupResult.Error}");
                }
                
                _logger.LogInformation("✅ Bank account setup completed successfully");
                
                // // Check current balance before requesting loan
                // _logger.LogInformation("💰 Checking current account balance...");
                // var currentBalance = await _bankClient.GetAccountBalanceAsync();
                // _logger.LogInformation("💰 Current account balance: {Balance}", currentBalance);
                
                // if (currentBalance == 0)
                // {
                //     _logger.LogInformation("💰 Balance is 0, requesting startup loan...");
                //     const decimal initialLoanAmount = 20000000m; // 20 million
                //     string? loanSuccess = await _bankClient.RequestLoanAsync(initialLoanAmount);
                //     if (loanSuccess == null)
                //     {
                //         _logger.LogWarning("⚠️ Initial loan request failed, trying with smaller amount...");
                //         // Try with a smaller amount if the initial request fails
                //         const decimal fallbackLoanAmount = 10000000m; // 10 million
                //         loanSuccess = await _bankClient.RequestLoanAsync(fallbackLoanAmount);
                //         if (loanSuccess == null)
                //         {
                //             _logger.LogError("❌ Failed to request startup loan with both amounts");
                //             return (false, null, "Failed to request startup loan");
                //         }
                //         _logger.LogInformation("✅ Startup loan requested successfully with fallback amount: {LoanNumber}", loanSuccess);
                //     }
                //     else
                //     {
                //         _logger.LogInformation("✅ Startup loan requested successfully: {LoanNumber}", loanSuccess);
                //     }
                // }
                // else
                // {
                //     _logger.LogInformation("💰 Balance is {Balance}, no loan needed", currentBalance);
                // }

                // Query and sync electronics machine details
                _logger.LogInformation("🔄 Syncing electronics machine details from THOH...");
                var machineDetailsSynced = await _machineDetailsService.SyncElectronicsMachineDetailsAsync();
                if (!machineDetailsSynced)
                {
                    _logger.LogWarning("⚠️ Could not sync electronics machine details from THOH. Continuing simulation startup.");
                }
                else
                {
                    _logger.LogInformation("✅ Electronics machine details synced.");
                }

                // Persist simulation start to the database
                await PersistSimulationStartAsync();
                
                _logger.LogInformation("✅ Simulation started successfully with bank account: {AccountNumber}", bankSetupResult.AccountNumber);
                return (true, bankSetupResult.AccountNumber, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during simulation startup");
                return (false, null, ex.Message);
            }
        }

        private async Task PersistSimulationStartAsync()
        {
            _logger.LogInformation("💾 Persisting simulation start to database");
            var sim = await _context.Simulations.FirstOrDefaultAsync();
            if (sim == null)
            {
                _logger.LogInformation("📝 Creating new simulation record in database");
                sim = new Models.Simulation
                {
                    IsRunning = true,
                    StartedAt = DateTime.UtcNow,
                    DayNumber = 1
                };
                _context.Simulations.Add(sim);
            }
            else
            {
                _logger.LogInformation("🔄 Updating existing simulation record in database");
                sim.IsRunning = true;
                sim.StartedAt = DateTime.UtcNow;
                sim.DayNumber = 1;
            }
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Simulation start persisted to database");
        }
    }
} 