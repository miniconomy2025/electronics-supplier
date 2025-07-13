using esAPI.Data;
using esAPI.Simulation.Tasks;
using esAPI.Services;
using esAPI.Clients;
using esAPI.Helpers;

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

            await ExecuteDailyTasksAsync(dayNumber);

            if (OnDayAdvanced != null)
                await OnDayAdvanced(dayNumber);
        }

        private async Task<bool> ExecuteStartupSequence()
        {

            const int maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(5);

            //try setting up notification url
            var setNotifyUrlAction = () => bankClient.SetNotificationUrlAsync();
            bool notifyUrlSuccess = await RetryHelper.TryExecuteAsync(setNotifyUrlAction, maxRetries, retryDelay);
            if (!notifyUrlSuccess)
            {
                // _logger.LogCritical("[Day 1] FAILED: Could not set notification URL after {Retries} attempts.", maxRetries);
                return false;
            }


            //Try creating bank account
            var ensureAccountAction = () => _bankAccountService.SetupBankAccount();
            var accountNumber = await RetryHelper.TryExecuteAsync(ensureAccountAction, maxRetries, retryDelay);

            if (accountNumber == null)
            {
                // _logger.LogCritical("[Day 1] FAILED: Could not set up bank account after {Retries} attempts.", maxRetries);
                return false;
            }

            var allPlans = await _costCalculator.GenerateAllPossibleStartupPlansAsync();
            var bestPlan = allPlans.OrderBy(p => p.TotalCost).FirstOrDefault();
            if (bestPlan == null)
            {
                return false;
            }

            //try requesting loan

            var requestLoanAction = () => bankClient.RequestLoanAsync(bestPlan.TotalCost);
            var loanNumber = await RetryHelper.TryExecuteAsync(requestLoanAction, maxRetries, retryDelay);
            if (string.IsNullOrEmpty(loanNumber))
            {
                // _logger.LogError("[Day 1] FAILED: Bank rejected or failed to process startup loan of {Amount:C} after {Retries} attempts.", bestPlan.TotalCost, maxRetries);
                return false;
            }


            return true;
        }

        private async Task ExecuteDailyTasksAsync(int dayNumber)
        {

            // 1. Query bank and store our balance
            await _bankService.GetAndStoreBalance(dayNumber);

            // 2. Check machine inventory and buy if none
            var machineTask = new MachineTask(_context);
            await machineTask.EnsureMachineAvailabilityAsync(dayNumber);

            // Add other tasks here later:
            // - MaterialTask
            // - ProductionTask
            // - OrderTask
            // 3. Run the main daily orchestration (acquiring materials, producing, etc.)
            await _dayOrchestrator.RunDayAsync(dayNumber);
        }

    }
}
