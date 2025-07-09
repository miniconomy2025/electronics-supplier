using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace esAPI.Services
{
    public class SimulationDayOrchestrator
    {
        private readonly BankService _bankService;
        private readonly InventoryService _inventoryService;
        private readonly MachineAcquisitionService _machineAcquisitionService;
        private readonly MaterialAcquisitionService _materialAcquisitionService;
        private readonly ProductionService _productionService;
        private readonly ILogger<SimulationDayOrchestrator> _logger;

        public SimulationDayOrchestrator(
            BankService bankService,
            InventoryService inventoryService,
            MachineAcquisitionService machineAcquisitionService,
            MaterialAcquisitionService materialAcquisitionService,
            ProductionService productionService,
            ILogger<SimulationDayOrchestrator> logger)
        {
            _bankService = bankService;
            _inventoryService = inventoryService;
            _machineAcquisitionService = machineAcquisitionService;
            _materialAcquisitionService = materialAcquisitionService;
            _productionService = productionService;
            _logger = logger;
        }

        public async Task RunDayAsync(int dayNumber)
        {
            _logger.LogInformation($"--- Simulation Day {dayNumber} Start ---");

            // 1. Check and store bank balance
            await _bankService.GetAndStoreBalance(dayNumber);

            // 2. Check and store inventory
            var inventory = await _inventoryService.GetAndStoreInventory();

            // 3. If no working machines, try to buy one
            if (inventory.Machines.InUse == 0)
            {
                var machineAvailable = await _machineAcquisitionService.CheckTHOHForMachines();
                if (machineAvailable)
                {
                    var (orderId, quantity) = await _machineAcquisitionService.PurchaseMachineViaBank();
                    await _machineAcquisitionService.QueryOrderDetailsFromTHOH();
                    if (orderId.HasValue && quantity > 0)
                    {
                        await _machineAcquisitionService.PlaceBulkLogisticsPickup(orderId.Value, quantity);
                    }
                }
            }

            // 4. Check raw materials
            bool hasCopper = inventory.MaterialsInStock.Any(m => m.MaterialName.ToLower() == "copper" && m.Quantity > 0);
            bool hasSilicon = inventory.MaterialsInStock.Any(m => m.MaterialName.ToLower() == "silicon" && m.Quantity > 0);
            if (!hasCopper || !hasSilicon)
            {
                await _materialAcquisitionService.PurchaseMaterialsViaBank();
                // await _materialAcquisitionService.PlaceBulkLogisticsPickup(); // No longer needed, handled inside service
            }

            // 5. Bulk logistics delivery is handled by /logistics endpoint

            // 6. Produce electronics
            var (created, materialsUsed) = await _productionService.ProduceElectronics();
            _logger.LogInformation($"Produced {created} electronics. Materials used: {string.Join(", ", materialsUsed.Select(kv => $"{kv.Key}: {kv.Value}"))}");

            _logger.LogInformation($"--- Simulation Day {dayNumber} End ---");
        }
    }
} 