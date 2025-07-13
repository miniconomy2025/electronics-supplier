using esAPI.Data;
using esAPI.Simulation.Tasks;
using esAPI.Services;
using esAPI.Clients;

namespace esAPI.Simulation
{
    public class SimulationEngine(AppDbContext context, BankService bankService, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator, IStartupCostCalculator costCalculator, ICommercialBankClient bankClient)
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;

        private readonly IStartupCostCalculator _costCalculator = costCalculator;

        private readonly BankService _bankService = bankService;

        private readonly ICommercialBankClient _bankClient = bankClient;

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
            if (dayNumber == 1)
            {
                await ExecuteStartupSequence();

            }
            Console.WriteLine($"Running simulation logic for Day {dayNumber}");

            // 1. Query bank and store our balance
            await _bankService.GetAndStoreBalance(dayNumber);


            // 2. Check machine inventory and buy if none
            var machineTask = new MachineTask(_context);
            await machineTask.EnsureMachineAvailabilityAsync(dayNumber);

            // Add other tasks here later:
            // - MaterialTask
            // - ProductionTask
            // - OrderTask
            if (OnDayAdvanced != null)
                await OnDayAdvanced(dayNumber);
        }

        private async Task<bool> ExecuteStartupSequence()
        {

            await _bankAccountService.SetupBankAccount();

            await _bankClient.SetNotificationUrlAsync();

            var allPlans = await _costCalculator.GenerateAllPossibleStartupPlansAsync();
            if (!allPlans.Any())
            {
                return false;
            }

            var bestPlan = allPlans.OrderBy(p => p.TotalCost).First();

            string? loanSuccess = await _bankClient.RequestLoanAsync(bestPlan.TotalCost);
            if (loanSuccess == null)
            {
                return false;
            }

            return true;
        }
    }
}
