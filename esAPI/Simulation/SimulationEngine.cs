using esAPI.Data;
using esAPI.Simulation.Tasks;
using esAPI.Services;

namespace esAPI.Simulation
{
    public class SimulationEngine
    {
        private readonly AppDbContext _context;
        private readonly BankAccountService _bankAccountService;

        public SimulationEngine(AppDbContext context, BankAccountService bankAccountService)
        {
            _context = context;
            _bankAccountService = bankAccountService;
        }

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
            if (dayNumber == 1)
            {
                await _bankAccountService.SetupBankAccount();
            }
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
