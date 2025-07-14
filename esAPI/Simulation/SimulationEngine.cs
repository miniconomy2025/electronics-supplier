using esAPI.Data;
using esAPI.Simulation.Tasks;
using esAPI.Services;
using esAPI.Clients;
using Microsoft.Extensions.Logging;

namespace esAPI.Simulation
{
    public class SimulationEngine(AppDbContext context, BankService bankService, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, IStartupCostCalculator costCalculator, ICommercialBankClient bankClient, ILogger<SimulationEngine> logger)
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;
        private readonly IStartupCostCalculator _costCalculator = costCalculator;
        private readonly BankService _bankService = bankService;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ILogger<SimulationEngine> _logger = logger;

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
            _logger.LogInformation("🏃‍♂️ Starting simulation day {DayNumber}", dayNumber);
            
            if (dayNumber == 1)
            {
                _logger.LogInformation("🎬 Executing startup sequence for day 1");
                await ExecuteStartupSequence();
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
                // Continue with simulation even if bank balance fails
            }

            // COMMENTED OUT: Other business logic for now
            /*
            // 2. Check machine inventory and buy if none
            _logger.LogInformation("🔧 Checking machine availability for day {DayNumber}", dayNumber);
            var machineTask = new MachineTask(_context);
            await machineTask.EnsureMachineAvailabilityAsync(dayNumber);
            _logger.LogInformation("✅ Machine availability check completed for day {DayNumber}", dayNumber);

            // Add other tasks here later:
            // - MaterialTask
            // - ProductionTask
            // - OrderTask
            */

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