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
        private readonly ICommercialBankClient _bankClient;
        private readonly ILogger<SimulationDayStartupService> _logger;

        public SimulationDayStartupService(
            ICommercialBankClient bankClient,
            ILogger<SimulationDayStartupService> logger)
        {
            _bankClient = bankClient;
            _logger = logger;
        }

        public Task<bool> ExecuteStartupSequenceAsync()
        {
            _logger.LogInformation("🏦 Day 1 startup sequence - bank account and initial loan already handled during simulation startup");

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

            _logger.LogInformation("✅ Day 1 startup sequence completed successfully - no additional actions needed");
            return Task.FromResult(true);
        }
    }
}
