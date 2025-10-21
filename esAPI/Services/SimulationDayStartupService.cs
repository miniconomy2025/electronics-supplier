using Microsoft.Extensions.Logging;
using esAPI.Interfaces;
using esAPI.Clients;

namespace esAPI.Services
{
    /// <summary>
    /// Service responsible for handling the startup sequence within a simulation day
    /// </summary>
    public class SimulationDayStartupService : ISimulationStartupService
    {
        private readonly SimulationDayOrchestrator _dayOrchestrator;
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<SimulationDayStartupService> _logger;

        public SimulationDayStartupService(
            SimulationDayOrchestrator dayOrchestrator,
            ICommercialBankClient bankClient,
            ILogger<SimulationDayStartupService> logger)
        {
            _dayOrchestrator = dayOrchestrator;
            _bankClient = bankClient;
            _logger = logger;
        }

        public async Task<bool> ExecuteStartupSequenceAsync()
        {
            _logger.LogInformation("🏦 Setting up bank account via day orchestrator");
            var bankSetupResult = await _dayOrchestrator.OrchestrateAsync();
            if (!bankSetupResult.Success && bankSetupResult.Error != "accountAlreadyExists")
            {
                _logger.LogError("❌ Failed to set up bank account: {Error}", bankSetupResult.Error);
                return false;
            }
            _logger.LogInformation("✅ Bank account setup completed: {AccountNumber}", bankSetupResult.AccountNumber);

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
