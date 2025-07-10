using esAPI.Data;
using esAPI.Simulation.Tasks;
using esAPI.Services;

namespace esAPI.Simulation
{
    public class SimulationEngine(AppDbContext context, BankAccountService bankAccountService, SimulationDayOrchestrator dayOrchestrator)
    {
        private readonly AppDbContext _context = context;
        private readonly BankAccountService _bankAccountService = bankAccountService;
        private readonly SimulationDayOrchestrator _dayOrchestrator = dayOrchestrator;

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
            if (dayNumber == 1)
            {
                await _bankAccountService.SetupBankAccount();
            }
            await _dayOrchestrator.RunDayAsync(dayNumber);
            Console.WriteLine($"Running simulation logic for Day {dayNumber}");

            // 1. Query bank and store our balance

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
    }
}
