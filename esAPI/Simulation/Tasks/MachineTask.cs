using esAPI.Data;
using esAPI.Models;
using esAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace esAPI.Simulation.Tasks
{
    public class MachineTask
    {
        private readonly AppDbContext _context;

        public MachineTask(AppDbContext context)
        {
            _context = context;
        }

        public async Task EnsureMachineAvailabilityAsync(int currentDay)
        {
            // Check if any working machine exists (not broken and not removed)
            var machineExists = await _context.Machines.AnyAsync(m =>
                m.MachineStatusId != (int)Models.Enums.Machine.Status.Broken && m.RemovedAt == null);

            Console.WriteLine($"Day {currentDay}: Machine exists? {machineExists}");

            // Call the reusable method to attempt machine purchase if needed
            await TryBuyMachineIfAffordableAsync(machineExists, currentDay);
        }

        private async Task TryBuyMachineIfAffordableAsync(bool machineExists, int currentDay)
        {
            Console.WriteLine($"Day {currentDay}: Starting machine purchase check...");

            // TODO: Check stored bank balance
            Console.WriteLine("TODO: Check bank balance");

            // TODO: Get cost of a new machine
            Console.WriteLine("TODO: Determine cost of a new machine");

            // Example placeholders for demonstration:
            decimal bankBalance = 10000m; // pretend bank balance
            decimal machineCost = 1500m;  // pretend machine cost

            Console.WriteLine($"Bank balance: {bankBalance}");
            Console.WriteLine($"Machine cost: {machineCost}");

            // Check conditions
            if (!machineExists)
            {
                Console.WriteLine("No working machine found. Will attempt to buy one.");
            }
            else if (machineCost <= bankBalance * 0.20m)
            {
                Console.WriteLine("Machine exists but cost is within 20% of bank balance. Will attempt to buy one.");
            }
            else
            {
                Console.WriteLine("Machine exists and cost exceeds 20% of bank balance. Will NOT buy a machine.");
                return;
            }

            // TODO: If condition met:
            //   - Create machine order and add to Machines table
            //   - Update bank balance (deduct machine cost)
            //   - Save changes to DB
            Console.WriteLine("TODO: Create machine order, update Machines table and bank balance");

            Console.WriteLine("Simulated: New machine added to the system.");
        }
    }
}
