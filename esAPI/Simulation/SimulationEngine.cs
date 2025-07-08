using esAPI.Data;
using esAPI.Simulation.Tasks;

namespace esAPI.Simulation
{
    public class SimulationEngine(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public static event Func<int, Task>? OnDayAdvanced;

        public async Task RunDayAsync(int dayNumber)
        {
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
