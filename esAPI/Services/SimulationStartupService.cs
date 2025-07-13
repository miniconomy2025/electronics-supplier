using Microsoft.EntityFrameworkCore;
using esAPI.Data;
using esAPI.Services;
using esAPI.Simulation;
using esAPI.Interfaces;

namespace esAPI.Services
{
    public class SimulationStartupService
    {
        private readonly AppDbContext _context;
        private readonly BankAccountService _bankAccountService;
        private readonly ISimulationStateService _stateService;
        private readonly OrderExpirationBackgroundService _orderExpirationBackgroundService;
        private readonly ILogger<SimulationStartupService> _logger;

        public SimulationStartupService(
            AppDbContext context,
            BankAccountService bankAccountService,
            ISimulationStateService stateService,
            OrderExpirationBackgroundService orderExpirationBackgroundService,
            ILogger<SimulationStartupService> logger)
        {
            _context = context;
            _bankAccountService = bankAccountService;
            _stateService = stateService;
            _orderExpirationBackgroundService = orderExpirationBackgroundService;
            _logger = logger;
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